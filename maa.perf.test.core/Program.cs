using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using maa.perf.test.core.Utils;
using CommandLine;

namespace maa.perf.test.core
{
    public class Program
    {
        private Options _options;
        private Maa.EnclaveInfo _enclaveInfo;
        private Maa.MaaService _maaService;

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

            Tracer.CurrentTracingLevel = _options.Verbose ? TracingLevel.Verbose : TracingLevel.Info;
            _enclaveInfo = Maa.EnclaveInfo.CreateFromFile(_options.EnclaveInfoFile);
            _maaService = new Maa.MaaService(_options);

            _options.Trace();
        }

        public async Task RunAsync()
        {
            // Handle ramp up if defined
            if (_options.RampUp > 4)
            {
                DateTime startTime = DateTime.Now;
                DateTime endTime = startTime + TimeSpan.FromSeconds(_options.RampUp);
                int numberIntervals = Math.Min(_options.RampUp / 5, 6);
                TimeSpan intervalLength = (endTime - startTime) / numberIntervals;
                double intervalRpsDelta = ((double)_options.TargetRPS) / ((double)numberIntervals);
                for (int i=0; i<numberIntervals; i++)
                {
                    long intervalRps = (long) Math.Round((i + 1) * intervalRpsDelta);
                    Tracer.TraceInfo($"Ramping up. RPS = {intervalRps}");
                    AsyncFor myRampUpFor = new AsyncFor(intervalRps, _options.AttestationProvider);
                    myRampUpFor.PerSecondMetricsAvailable += new ConsoleMetricsHandler().MetricsAvailableHandler;
                    await myRampUpFor.For(intervalLength, _options.SimultaneousConnections, GetRestApiCallback());
                }
            }

            Tracer.TraceInfo($"Running at RPS = {_options.TargetRPS}");
            AsyncFor myFor = new AsyncFor(_options.TargetRPS, _options.AttestationProvider);
            myFor.PerSecondMetricsAvailable += new ConsoleMetricsHandler().MetricsAvailableHandler;
            myFor.PerSecondMetricsAvailable += new CsvFileMetricsHandler().MetricsAvailableHandler;
            await myFor.For(TimeSpan.MaxValue, _options.SimultaneousConnections, GetRestApiCallback());
        }

        public Func<Task<double>> GetRestApiCallback()
        {
            if (!string.IsNullOrEmpty(_options.Url))
            {
                return GetUrlAsync;
            }
            else
            {
                switch (_options.RestApi)
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
    }
}
