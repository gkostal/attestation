using CommandLine;
using maa.perf.test.core.Maa;
using maa.perf.test.core.Model;
using maa.perf.test.core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

// TODO: 
//   * Let .NET FX control also control max connections
//         * So 4 tests might each try to use 10 connections, but the .NET FX might limit the total to 10
//         * This way fast tests (e.g. get open id metadata) will allow other tests to use some of their connection quota
namespace maa.perf.test.core
{
    public class Program
    {
        private readonly Options _options;
        private MixInfo _mixInfo;
        private readonly List<AsyncFor> _asyncForInstances = new List<AsyncFor>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

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
                _options.RestApi = Api.AttestSgxEnclave;
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
                for (var i = 0; i < _mixInfo.TestRuns.Count && !_cancellationTokenSource.IsCancellationRequested; i++)
                {
                    var testRunInfo = _mixInfo.TestRuns[i];
                    var asyncRunners = new List<Task>();

                    uberCsvAggregator.SetRpsAndConnections(testRunInfo.TargetRPS, testRunInfo.SimultaneousConnections);

                    Tracer.TraceInfo("");
                    Tracer.TraceInfo($"Starting test run #{i}   RPS: {testRunInfo.TargetRPS}  Connections: {testRunInfo.SimultaneousConnections}");

                    // Handle ramp up if needed
                    await RampUpAsync(testRunInfo);

                    // Initiate separate asyncfor for each API in the mix
                    if (!_cancellationTokenSource.IsCancellationRequested)
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
                            asyncRunners.Add(myFor.ForAsync(
                                TimeSpan.FromSeconds(testRunInfo.TestTimeSeconds), 
                                testRunInfo.SimultaneousConnections, 
                                new MaaServiceApiCaller(apiInfo, _mixInfo.ProviderMix, testRunInfo.EnclaveInfoFile, testRunInfo.ForceReconnects).CallApi,
                                _cancellationTokenSource.Token));
                        }
                    }

                    // Wait for all to be complete (happens when they finish or crtl-c is hit)
                    try
                    {
                        await Task.WhenAll(asyncRunners.ToArray());
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore task cancelled if we requested cancellation via ctrl-c
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            Tracer.TraceInfo(($"Organized shutdown in progress.  All {asyncRunners.Count} asyncfor instances have gracefully shutdown."));
                        }
                        else
                        {
                            throw;
                        }
                    }

                    //Tracer.TraceInfo("");
                    Tracer.TraceInfo($"Completed test run #{i}   RPS: {testRunInfo.TargetRPS}  Connections: {testRunInfo.SimultaneousConnections}");
                }
            }
            Tracer.TraceInfo($"Organized shutdown complete.");

            // Print out exception history
            MaaServiceApiCaller.TraceExceptionHistory();
        }

        private async Task RampUpAsync(TestRunInfo testRunInfo)
        {
            // Handle ramp up if defined
            if (testRunInfo.RampUpTimeSeconds > 4 && !_cancellationTokenSource.IsCancellationRequested)
            {
                Tracer.TraceInfo($"Ramping up starts.");

                DateTime startTime = DateTime.Now;
                DateTime endTime = startTime + TimeSpan.FromSeconds(testRunInfo.RampUpTimeSeconds);
                int numberIntervals = Math.Min(testRunInfo.RampUpTimeSeconds / 5, 6);
                TimeSpan intervalLength = (endTime - startTime) / numberIntervals;
                double intervalRpsDelta = ((double)testRunInfo.TargetRPS) / ((double)numberIntervals);
                for (int i = 0; i < numberIntervals && !_cancellationTokenSource.IsCancellationRequested; i++)
                {
                    var apiInfo = _mixInfo.ApiMix[i % _mixInfo.ApiMix.Count];

                    long intervalRps = (long)Math.Round((i + 1) * intervalRpsDelta);
                    Tracer.TraceInfo($"Ramping up. RPS = {intervalRps}");

                    AsyncFor myRampUpFor = new AsyncFor(intervalRps, GetResourceDescription(apiInfo, _mixInfo), GetTestDescription(apiInfo), testRunInfo.MeasureServerSideTime);
                    myRampUpFor.PerSecondMetricsAvailable += new ConsoleMetricsHandler().MetricsAvailableHandler;
                    _asyncForInstances.Add(myRampUpFor);

                    try
                    {
                        await myRampUpFor.ForAsync(
                            intervalLength,
                            testRunInfo.SimultaneousConnections,
                            new MaaServiceApiCaller(apiInfo, _mixInfo.ProviderMix, testRunInfo.EnclaveInfoFile, testRunInfo.ForceReconnects).CallApi,
                            _cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore task cancelled if we requested cancellation via ctrl-c
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            Tracer.TraceInfo(($"Organized shutdown in progress.  All asyncfor instances have gracefully shutdown."));
                        }
                        else
                        {
                            throw;
                        }
                    }
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
            Tracer.TraceInfo("");
            Tracer.TraceInfo($"Organized shutdown starting.  Informing {_asyncForInstances.Count} asyncfor instances to terminate.");

            // Do NOT stop running application
            e.Cancel = true;

            // Instead, tell all asyncfor tasks to cancel their work now
            _cancellationTokenSource.Cancel();
        }
    }
}
