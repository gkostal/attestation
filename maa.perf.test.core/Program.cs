using CommandLine;
using maa.perf.test.core.Maa;
using maa.perf.test.core.Model;
using maa.perf.test.core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

// TODO: 
//   * Let .NET FX control also control max connections
//         * So 4 tests might each try to use 10 connections, but the .NET FX might limit the total to 10
//         * This way fast tests (e.g. get open id metadata) will allow other tests to use some of their connection quota
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
        }

        public async Task RunAsync()
        {
            // Complete all HTTP conversations before exiting application
            Console.CancelKeyPress += HandleControlC;

            // Retrieve all run configuration settings
            _mixInfo = _options.GetMixInfo();
            _mixInfo.Trace();

            using (var uberCsvAggregator = new CsvAggregatingMetricsHandler())
            {
                for (int i = 0; i < _mixInfo.TestRuns.Count && !_terminate; i++)
                {
                    var testRunInfo = _mixInfo.TestRuns[i];
                    var asyncRunners = new List<Task>();

                    uberCsvAggregator.SetRpsAndConnections(testRunInfo.TargetRPS, testRunInfo.SimultaneousConnections);

                    Tracer.TraceInfo("");
                    Tracer.TraceInfo($"Starting test run #{i}   RPS: {testRunInfo.TargetRPS}  Connections: {testRunInfo.SimultaneousConnections}");

                    // Handle ramp up if needed
                    await RampUpAsync(testRunInfo);

                    // Initiate separate asyncfor for each API in the mix
                    if (!_terminate)
                    {
                        foreach (var apiInfo in _mixInfo.ApiMix)
                        {
                            var myFor = new AsyncFor(testRunInfo.TargetRPS * apiInfo.Percentage, GetResourceDescription(apiInfo, _mixInfo), GetTestDescription(apiInfo), testRunInfo.MeasureServerSideTime);
                            if (_mixInfo.ApiMix.Count > 1)
                            {
                                myFor.PerSecondMetricsAvailable += new ConsoleAggregatingMetricsHandler(_mixInfo.ApiMix.Count, 60).MetricsAvailableHandler;
                            }
                            else
                            {
                                myFor.PerSecondMetricsAvailable += new ConsoleMetricsHandler().MetricsAvailableHandler;
                            }

                            myFor.PerSecondMetricsAvailable += new CsvFileMetricsHandler().MetricsAvailableHandler;
                            if (testRunInfo.TestTimeSeconds != int.MaxValue)
                            {
                                myFor.PerSecondMetricsAvailable += uberCsvAggregator.MetricsAvailableHandler;
                            }

                            _asyncForInstances.Add(myFor);
                            asyncRunners.Add(myFor.For(TimeSpan.FromSeconds(testRunInfo.TestTimeSeconds), testRunInfo.SimultaneousConnections, new MaaServiceApiCaller(apiInfo, _mixInfo.ProviderMix, testRunInfo.EnclaveInfoFile, testRunInfo.ForceReconnects).CallApi));
                        }
                    }

                    // Wait for all to be complete (happens when crtl-c is hit)
                    await Task.WhenAll(asyncRunners.ToArray());

                    Tracer.TraceInfo("");
                    Tracer.TraceInfo($"Completed test run #{i}   RPS: {testRunInfo.TargetRPS}  Connections: {testRunInfo.SimultaneousConnections}");
                }
            }
            Tracer.TraceInfo($"Organized shutdown complete.");
        }

        private async Task RampUpAsync(TestRunInfo testRunInfo)
        {
            // Handle ramp up if defined
            if (testRunInfo.RampUpTimeSeconds > 4 && !_terminate)
            {
                Tracer.TraceInfo($"Ramping up starts.");

                DateTime startTime = DateTime.Now;
                DateTime endTime = startTime + TimeSpan.FromSeconds(testRunInfo.RampUpTimeSeconds);
                int numberIntervals = Math.Min(testRunInfo.RampUpTimeSeconds / 5, 6);
                TimeSpan intervalLength = (endTime - startTime) / numberIntervals;
                double intervalRpsDelta = ((double)testRunInfo.TargetRPS) / ((double)numberIntervals);
                for (int i = 0; i < numberIntervals && !_terminate; i++)
                {
                    var apiInfo = _mixInfo.ApiMix[i % _mixInfo.ApiMix.Count];

                    long intervalRps = (long)Math.Round((i + 1) * intervalRpsDelta);
                    Tracer.TraceInfo($"Ramping up. RPS = {intervalRps}");

                    AsyncFor myRampUpFor = new AsyncFor(intervalRps, GetResourceDescription(apiInfo, _mixInfo), GetTestDescription(apiInfo), testRunInfo.MeasureServerSideTime);
                    myRampUpFor.PerSecondMetricsAvailable += new ConsoleMetricsHandler().MetricsAvailableHandler;
                    _asyncForInstances.Add(myRampUpFor);

                    await myRampUpFor.For(intervalLength, testRunInfo.SimultaneousConnections, new MaaServiceApiCaller(apiInfo, _mixInfo.ProviderMix, testRunInfo.EnclaveInfoFile, testRunInfo.ForceReconnects).CallApi);
                }
                Tracer.TraceInfo($"Ramping up complete.");
            }
        }

        private string GetResourceDescription(ApiInfo apiInfo, MixInfo mixInfo)
        {
            if (string.IsNullOrEmpty(apiInfo.Url))
            {
                var description = mixInfo.ProviderMix[0].DnsName;
                var totalProviderCount = mixInfo.ProviderMix.Sum(p => p.ProviderCount);

                if (totalProviderCount > 1)
                {
                    description = $"{description} + {totalProviderCount - 1} more";
                }

                return description;
            }
            else
            {
                return apiInfo.Url;
            }
        }

        private string GetTestDescription(ApiInfo apiInfo)
        {
            return string.IsNullOrEmpty(apiInfo.Url) ? apiInfo.ApiName.ToString() : "GetUrl";
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

            // Print out exception history
            lock (MaaServiceApiCaller._exceptionHistory)
            {
                Tracer.TraceWarning($"Exception Summary");
                foreach (var x in MaaServiceApiCaller._exceptionHistory)
                {
                    Tracer.TraceWarning($"{x.Value,10} : {x.Key}");
                }
            }
        }
    }
}
