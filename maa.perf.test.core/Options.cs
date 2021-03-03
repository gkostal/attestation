using maa.perf.test.core.Utils;
using CommandLine;

namespace maa.perf.test.core
{
    public class Options
    {
        [Option('p', "provider", Required = false, HelpText = "Attestation provider DNS name")]
        public string AttestationProvider { get; set; }

        [Option('c', "connections", Required = false, HelpText = "Number of simultaneous connections (and calls) to the MAA service")]
        public long SimultaneousConnections { get; set; }

        [Option('r', "rps", Required = false, HelpText = "Target RPS")]
        public long TargetRPS { get; set; }

        [Option('f', "forcereconnects", Required = false, HelpText = "Force reconnects on each request")]
        public bool ForceReconnects { get; set; }

        [Option('w', "previewapiversion", Required = false, HelpText = "Use preview api-version instead of GA")]
        public bool UsePreviewApiVersion { get; set; }

        [Option('a', "api", Required = false, HelpText = "REST Api to test: {AttestSgx, AttestOpenEnclave, GetOpenIdConfiguration, GetCerts, GetServiceHealth}")]
        public Api RestApi { get; set; }

        [Option('q', "quote", Required = false, HelpText = "Enclave info file containing the SGX quote")]
        public string EnclaveInfoFile { get; set; }

        [Option('o', "port", Required = false, HelpText = "Override service port number (default is 443)")]
        public string ServicePort { get; set; }

        [Option('t', "tenant", Required = false, HelpText = "Override tenant name (default extracted from DNS name)")]
        public string TenantName { get; set; }

        [Option('h', "http", Required = false, HelpText = "Connect via HTTP (default is HTTPS)")]
        public bool UseHttp { get; set; }

        [Option('u', "url", Required = false, HelpText = "Load test a HTTP GET request for the provided URL")]
        public string Url { get; set; }

        [Option('m', "rampup", Required = false, HelpText = "Ramp up time in seconds")]
        public int RampUp { get; set; }

        [Option('z', "providercount", Required = false, HelpText = "Provider count (default = 1)")]
        public int ProviderCount { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages")]
        public bool Verbose { get; set; }

        public Options()
        {
            Verbose = false;
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
            Url = null;
            RampUp = 0;
            ProviderCount = 1;
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
            Tracer.TraceInfo($"Url                      : {Url}");
            Tracer.TraceInfo($"RampUp                   : {RampUp}");
            Tracer.TraceInfo($"");
        }
    }
}