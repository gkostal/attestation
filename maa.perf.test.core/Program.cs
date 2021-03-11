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
                    // TODO: Fix warmup
                    //await myRampUpFor.For(intervalLength, _options.SimultaneousConnections, GetRestApiCallback());
                }
            }

            List<Task> asyncRunners = new List<Task>();
            var mixInfo = _options.GetMixInfo();

            foreach (var apiInfo in mixInfo.ApiMix)
            {
                var rps = _options.TargetRPS * apiInfo.Percentage;

                Tracer.TraceInfo($"Running {apiInfo.ApiName} at RPS = {rps}");
                AsyncFor myFor = new AsyncFor(rps, _options.AttestationProvider);
                myFor.PerSecondMetricsAvailable += new ConsoleAggregattingMetricsHandler(mixInfo.ApiMix.Count, 60).MetricsAvailableHandler;
                //myFor.PerSecondMetricsAvailable += new CsvFileMetricsHandler().MetricsAvailableHandler;

                _asyncForInstances.Add(myFor);
                asyncRunners.Add(myFor.For(TimeSpan.MaxValue, _options.SimultaneousConnections, new MaaServiceApiCaller(apiInfo, mixInfo.ProviderMix, _options.EnclaveInfoFile, _options.ForceReconnects).CallApi));
            }

            await Task.WhenAll(asyncRunners.ToArray());
            Tracer.TraceInfo($"Organized shutdown complete.");
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
    }
}
