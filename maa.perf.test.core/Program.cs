using CommandLine;
using maa.perf.test.core.Maa;
using maa.perf.test.core.Model;
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
        private MixInfo _mixInfo;
        private List<AsyncFor> _asyncForInstances = new List<AsyncFor>();
        private bool _terminate = false;

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

            _options.Trace();
        }

        public async Task RunAsync()
        {
            // Complete all HTTP conversations before exiting application
            Console.CancelKeyPress += HandleControlC;

            // Housekeeping
            List<Task> asyncRunners = new List<Task>();
            _mixInfo = _options.GetMixInfo();

            // Handle ramp up if needed
            await RampUpAsync();

            // Initiate separate asyncfor for each API in the mix
            if (!_terminate)
            {
                foreach (var apiInfo in _mixInfo.ApiMix)
                {
                    var myFor = new AsyncFor(_options.TargetRPS * apiInfo.Percentage, GetProviderMixDescription(_mixInfo), apiInfo.ApiName.ToString());
                    if (_mixInfo.ApiMix.Count > 1)
                    {
                        myFor.PerSecondMetricsAvailable += new ConsoleAggregatingMetricsHandler(_mixInfo.ApiMix.Count, 60).MetricsAvailableHandler;
                    }
                    else
                    {
                        myFor.PerSecondMetricsAvailable += new ConsoleMetricsHandler().MetricsAvailableHandler;
                        myFor.PerSecondMetricsAvailable += new CsvFileMetricsHandler().MetricsAvailableHandler;
                    }

                    _asyncForInstances.Add(myFor);
                    asyncRunners.Add(myFor.For(TimeSpan.FromSeconds(_options.TestTimeSeconds), _options.SimultaneousConnections, new MaaServiceApiCaller(apiInfo, _mixInfo.ProviderMix, _options.EnclaveInfoFile, _options.ForceReconnects).CallApi));
                }
            }

            // Wait for all to be complete (happens when crtl-c is hit)
            await Task.WhenAll(asyncRunners.ToArray());
            Tracer.TraceInfo($"Organized shutdown complete.");
        }

        private async Task RampUpAsync()
        {
            // Handle ramp up if defined
            if (_options.RampUpTimeSeconds > 4 && !_terminate)
            {
                Tracer.TraceInfo($"Ramping up starts.");

                DateTime startTime = DateTime.Now;
                DateTime endTime = startTime + TimeSpan.FromSeconds(_options.RampUpTimeSeconds);
                int numberIntervals = Math.Min(_options.RampUpTimeSeconds / 5, 6);
                TimeSpan intervalLength = (endTime - startTime) / numberIntervals;
                double intervalRpsDelta = ((double)_options.TargetRPS) / ((double)numberIntervals);
                for (int i = 0; i < numberIntervals && !_terminate; i++)
                {
                    var apiInfo = _mixInfo.ApiMix[i % _mixInfo.ApiMix.Count];

                    long intervalRps = (long)Math.Round((i + 1) * intervalRpsDelta);
                    Tracer.TraceInfo($"Ramping up. RPS = {intervalRps}");

                    AsyncFor myRampUpFor = new AsyncFor(intervalRps, GetProviderMixDescription(_mixInfo), $"{apiInfo.ApiName}");
                    myRampUpFor.PerSecondMetricsAvailable += new ConsoleMetricsHandler().MetricsAvailableHandler;
                    _asyncForInstances.Add(myRampUpFor);

                    await myRampUpFor.For(intervalLength, _options.SimultaneousConnections, new MaaServiceApiCaller(apiInfo, _mixInfo.ProviderMix, _options.EnclaveInfoFile, _options.ForceReconnects).CallApi);
                }
                Tracer.TraceInfo($"Ramping up complete.");
            }
        }

        private string GetProviderMixDescription(MixInfo mixInfo)
        {
            var description = mixInfo.ProviderMix[0].DnsName;
            
            if (mixInfo.ProviderMix.Count > 1)
            {
                description = $"{description} + {mixInfo.ProviderMix.Count - 1} more";
            }

            return description;
        }

        private void HandleControlC(object sender, ConsoleCancelEventArgs e)
        {
            Tracer.TraceInfo($"Organized shutdown starting.  Informing {_asyncForInstances.Count} asyncfor instances to terminate.\n");

            // Do NOT stop running application yet
            _terminate = true;
            e.Cancel = true;

            // Tell all asyncfor instances to stop
            foreach (var af in _asyncForInstances)
            {
                af.Terminate();
            }
        }
    }
}
