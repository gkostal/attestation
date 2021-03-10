using CommandLine;
using maa.perf.test.core.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace maa.perf.test.core
{
    public class Program
    {
        private Options _options;
        private Maa.EnclaveInfo _enclaveInfo;
        private Maa.MaaService _maaService;
        private Random _rnd = new Random();
        private List<AsyncFor> _asyncForInstances = new List<AsyncFor>();

        public static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 1024 * 32;

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    new Program(o).RunAsync().Wait();
                });
        }

        public Program(Options options)
        {
            _options = options;
            if ((string.IsNullOrEmpty(_options.MixFileName)) && (_options.RestApi == Api.None))
            {
                _options.RestApi = Api.AttestSgx;
            }

            Tracer.CurrentTracingLevel = _options.Verbose ? TracingLevel.Verbose : TracingLevel.Info;
            _enclaveInfo = Maa.EnclaveInfo.CreateFromFile(_options.EnclaveInfoFile);
            _maaService = new Maa.MaaService(_options);

            _options.Trace();
        }

        public async Task RunAsync()
        {
            // Complete all HTTP conversations before exiting application
            Console.CancelKeyPress += HandleControlC;

            // Handle ramp up if defined
            if (_options.RampUp > 4)
            {
                DateTime startTime = DateTime.Now;
                DateTime endTime = startTime + TimeSpan.FromSeconds(_options.RampUp);
                int numberIntervals = Math.Min(_options.RampUp / 5, 6);
                TimeSpan intervalLength = (endTime - startTime) / numberIntervals;
                double intervalRpsDelta = ((double)_options.TargetRPS) / ((double)numberIntervals);
                for (int i = 0; i < numberIntervals; i++)
                {
                    long intervalRps = (long)Math.Round((i + 1) * intervalRpsDelta);
                    Tracer.TraceInfo($"Ramping up. RPS = {intervalRps}");
                    AsyncFor myRampUpFor = new AsyncFor(intervalRps, _options.AttestationProvider);
                    myRampUpFor.PerSecondMetricsAvailable += new ConsoleMetricsHandler().MetricsAvailableHandler;
                    await myRampUpFor.For(intervalLength, _options.SimultaneousConnections, GetRestApiCallback());
                }
            }

            // Are we in mix mode?
            if (!string.IsNullOrEmpty(_options.MixFileName))
            {
                List<Task> asyncRunners = new List<Task>();

                foreach (var a in _options.MixFileContent.ApiMix)
                {
                    var rps = _options.TargetRPS * a.Percentage;

                    Tracer.TraceInfo($"Running {a.ApiName} at RPS = {rps}");
                    AsyncFor myFor = new AsyncFor(rps, _options.AttestationProvider);
                    myFor.PerSecondMetricsAvailable += new ConsoleAggregattingMetricsHandler(_options.MixFileContent.ApiMix.Count, 60).MetricsAvailableHandler;
                    //myFor.PerSecondMetricsAvailable += new CsvFileMetricsHandler().MetricsAvailableHandler;

                    _asyncForInstances.Add(myFor);
                    asyncRunners.Add(myFor.For(TimeSpan.MaxValue, _options.SimultaneousConnections, GetCallback(a.ApiName)));
                }

                Task.WaitAll(asyncRunners.ToArray());
            }
            else
            {
                Tracer.TraceInfo($"Running at RPS = {_options.TargetRPS}");
                AsyncFor myFor = new AsyncFor(_options.TargetRPS, _options.AttestationProvider);
                myFor.PerSecondMetricsAvailable += new ConsoleMetricsHandler().MetricsAvailableHandler;
                myFor.PerSecondMetricsAvailable += new CsvFileMetricsHandler().MetricsAvailableHandler;

                _asyncForInstances.Add(myFor);
                await myFor.For(TimeSpan.MaxValue, _options.SimultaneousConnections, GetRestApiCallback());
            }

            Tracer.TraceInfo($"Organized shutdown complete.");
        }

        public async Task<double> CallAttestSgxPreviewApiVersionAsync()
        {
            return await WrapServiceCallAsync(async () => await _maaService.AttestOpenEnclaveAsync(new Maa.Preview.AttestOpenEnclaveRequestBody(_enclaveInfo)));
        }

        public async Task<double> CallAttestSgxGaApiVersionAsync()
        {
            return await WrapServiceCallAsync(async () => await _maaService.AttestOpenEnclaveAsync(new Maa.Ga.AttestOpenEnclaveRequestBody(_enclaveInfo)));
        }

        public async Task<double> GetCertsAsync()
        {
            return await WrapServiceCallAsync(async () => await _maaService.GetCertsAsync());
        }

        public async Task<double> GetOpenIdConfigurationAsync()
        {
            return await WrapServiceCallAsync(async () => await _maaService.GetOpenIdConfigurationAsync());
        }

        public async Task<double> GetServiceHealthAsync()
        {
            return await WrapServiceCallAsync(async () => await _maaService.GetServiceHealthAsync());
        }

        public async Task<double> GetUrlAsync()
        {
            return await WrapServiceCallAsync(async () => await _maaService.GetUrlAsync(_options.Url));
        }

        private void HandleControlC(object sender, ConsoleCancelEventArgs e)
        {
            Tracer.TraceInfo($"Organized shutdown starting.  Informing {_asyncForInstances.Count} asyncfor instances to terminate.\n");

            // Do NOT stop running application yet
            e.Cancel = true;

            // Tell all asyncfor instances to stop
            foreach (var af in _asyncForInstances)
            {
                af.Terminate();
            }
        }

        private async Task<double> WrapServiceCallAsync(Func<Task<string>> callServiceAsync)
        {
            try
            {
                await callServiceAsync();
            }
            catch (Exception x)
            {
                Tracer.TraceError($"Exception caught: {x.ToString()}");
            }

            return await Task.FromResult(0.0);
        }

        public async Task<double> CallMixApiSet()
        {
            var sampleValue = _rnd.NextDouble();
            var currentSum = 0.0d;

            foreach (var a in _options.MixFileContent.ApiMix)
            {
                if (sampleValue < currentSum + a.Percentage)
                {
                    return await GetCallback(a.ApiName)();
                }
                currentSum += a.Percentage;
            }

            // Make sure rounding error doesn't fall through without calling an API
            return await GetCallback(_options.MixFileContent.ApiMix[_options.MixFileContent.ApiMix.Count - 1].ApiName)();
        }

        private Func<Task<double>> GetCallback(Api theApi)
        {
            switch (theApi)
            {
                case Api.AttestOpenEnclave:
                    if (_options.UsePreviewApiVersion)
                        return CallAttestSgxPreviewApiVersionAsync;
                    else
                        return CallAttestSgxGaApiVersionAsync;
                case Api.AttestSgx:
                    return CallAttestSgxGaApiVersionAsync;
                case Api.GetCerts:
                    return GetCertsAsync;
                case Api.GetOpenIdConfiguration:
                    return GetOpenIdConfigurationAsync;
                case Api.GetServiceHealth:
                    return GetServiceHealthAsync;
                default:
                    return CallAttestSgxGaApiVersionAsync;
            }
        }

        private Func<Task<double>> GetRestApiCallback()
        {
            if (!string.IsNullOrEmpty(_options.Url))
            {
                return GetUrlAsync;
            }
            else
            {
                if (_options.RestApi == Api.None)
                {
                    return CallMixApiSet;
                }
                else
                {
                    return GetCallback(_options.RestApi);
                }
            }
        }

    }
}
