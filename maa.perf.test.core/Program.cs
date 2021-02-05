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
        public enum Api
        {
            AttestSgx,
            AttestOpenEnclave,
            GetOpenIdConfiguration,
            GetCerts,
            GetServiceHealth
        };

        public class Options
        {
            [Option('p', "provider", Required = false, HelpText = "Attestation provider DNS name.")]
            public string AttestationProvider { get; set; }

            [Option('c', "connections", Required = false, HelpText = "Number of simultaneous connections (and calls) to the MAA service.")]
            public long  SimultaneousConnections { get; set; }

            [Option('r', "rps", Required = false, HelpText = "Target RPS.")]
            public long TargetRPS { get; set; }

            [Option('f', "forcereconnects", Required = false, HelpText = "Force reconnects on each request.")]
            public bool ForceReconnects { get; set; }

            [Option('w', "previewapiversion", Required = false, HelpText = "Use preview api-version instead of GA.")]
            public bool UsePreviewApiVersion { get; set; }

            [Option('a', "api", Required = false, HelpText = "REST Api to test: {AttestSgx, AttestOpenEnclave, GetOpenIdConfiguration, GetCerts, GetServiceHealth}")]
            public Api RestApi { get; set; }

            [Option('q', "quote", Required = false, HelpText = "Enclave info file containing the SGX quote.")]
            public string EnclaveInfoFile { get; set; }

            [Option('o', "port", Required = false, HelpText = "Override service port number (default is 443).")]
            public string ServicePort { get; set; }

            [Option('t', "tenant", Required = false, HelpText = "Override tenant name (default extracted from DNS name).")]
            public string TenantName { get; set; }

            [Option('h', "http", Required = false, HelpText = "Connect via HTTP (default is HTTPS).")]
            public bool UseHttp { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            public Options()
            {
                Verbose = true;
                AttestationProvider = "shareduks.uks.attest.azure.net";
                EnclaveInfoFile = "./Quotes/enclave.info.release.json";
                SimultaneousConnections = 5;
                TargetRPS = 1;
                ForceReconnects = false;
                UsePreviewApiVersion = false;
                ServicePort = "443";
                UseHttp = false;
                TenantName = null;
                RestApi = Api.AttestOpenEnclave;
            }

            public void Trace()
            {
                Tracer.TraceInfo($"");
                Tracer.TraceInfo($"Attestation Provider     : {AttestationProvider}");
                Tracer.TraceInfo($"REST Api                 : {RestApi}");
                Tracer.TraceInfo($"Enclave Info File        : {EnclaveInfoFile}");
                Tracer.TraceInfo($"Simultaneous Connections : {SimultaneousConnections}");
                Tracer.TraceInfo($"Target RPS               : {TargetRPS}");
                Tracer.TraceInfo($"Force Reconnects         : {ForceReconnects}");
                Tracer.TraceInfo($"Use Preview API Version  : {UsePreviewApiVersion}");
                Tracer.TraceInfo($"Service port             : {ServicePort}");
                Tracer.TraceInfo($"Tenant Name Override     : {TenantName}");
                Tracer.TraceInfo($"Use HTTP                 : {UseHttp}");
                Tracer.TraceInfo($"");
            }
        }

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

            Tracer.CurrentTracingLevel = _options.Verbose ? TracingLevel.Verbose : TracingLevel.Warning;
            _enclaveInfo = Maa.EnclaveInfo.CreateFromFile(_options.EnclaveInfoFile);
            _maaService = new Maa.MaaService(_options.AttestationProvider, _options.ForceReconnects, _options.ServicePort, _options.TenantName, _options.UseHttp);

            _options.Trace();
        }

        public async Task RunAsync()
        {
            AsyncFor myFor = new AsyncFor(_options.TargetRPS, _options.AttestationProvider);
            myFor.PerSecondMetricsAvailable += new ConsoleMetricsHandler().MetricsAvailableHandler;
            myFor.PerSecondMetricsAvailable += new CsvFileMetricsHandler().MetricsAvailableHandler;
            await myFor.For(TimeSpan.MaxValue, _options.SimultaneousConnections, GetRestApiCallback());
        }

        public Func<Task<double>> GetRestApiCallback()
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
