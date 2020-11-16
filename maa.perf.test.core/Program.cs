using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using maa.perf.test.core.Utils;
using CommandLine;
using maa.perf.test.core.Maa;
using maa.perf.test.core.Authentication;

namespace maa.perf.test.core
{
    class Program
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option('p', "provider", Required = false, HelpText = "Attestation provider DNS name.")]
            public string AttestationProvider { get; set; }

            [Option('q', "quote", Required = false, HelpText = "Enclave info file containing the SGX quote.")]
            public string EnclaveInfoFile { get; set; }

            [Option('c', "connections", Required = false, HelpText = "Number of simultaneous connections (and calls) to the MAA service.")]
            public long  SimultaneousConnections { get; set; }

            [Option('r', "rps", Required = false, HelpText = "Target RPS.")]
            public long TargetRPS { get; set; }

            [Option('f', "forcereconnects", Required = false, HelpText = "Force reconnects on each request.")]
            public bool ForceReconnects { get; set; }

            public Options()
            {
                Verbose = true;
                AttestationProvider = "shareduks.uks.attest.azure.net";
                //AttestationProvider = "sharedeus.eus.test.attest.azure.net";
                EnclaveInfoFile = "./Quotes/enclave.info.release.json";
                SimultaneousConnections = 30;
                TargetRPS = 10;
            }
        }

        private Options _options;
        private EnclaveInfo _enclaveInfo;
        private MaaService _maaService;

        static void Main(string[] args)
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

            Tracer.CurrentTracingLevel = _options.Verbose ? TracingLevel.Verbose : TracingLevel.Warning;
            _enclaveInfo = EnclaveInfo.CreateFromFile(_options.EnclaveInfoFile);
            _maaService = new MaaService(_options.AttestationProvider, _options.ForceReconnects);

            Tracer.TraceInfo($"Attestation Provider     : {_options.AttestationProvider}");
            Tracer.TraceInfo($"Enclave Info File        : {_options.EnclaveInfoFile}");
            Tracer.TraceInfo($"Simultaneous Connections : {_options.SimultaneousConnections}");
            Tracer.TraceInfo($"Target RPS               : {_options.TargetRPS}");
            Tracer.TraceInfo($"Force Reconnects         : {_options.ForceReconnects}");
        }

        public async Task RunAsync()
        {
            AsyncFor myFor = new AsyncFor(_options.TargetRPS, "MAA SGX Attest");
            myFor.PerSecondMetricsAvailable += new ConsoleMetricsHandler().MetricsAvailableHandler;
            myFor.PerSecondMetricsAvailable += new CsvFileMetricsHandler().MetricsAvailableHandler;
            await myFor.For(TimeSpan.MaxValue, _options.SimultaneousConnections, CallAttestSgx);
        }

        public async Task<double> CallAttestSgx()
        {
            try
            {
                await _maaService.AttestOpenEnclaveAsync(_enclaveInfo.GetMaaBody());
            }
            catch (Exception x)
            {
                Tracer.TraceError($"Exception caught: {x.ToString()}");
            }

            return await Task.FromResult(0.0);
        }
    }
}
