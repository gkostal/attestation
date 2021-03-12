using CommandLine;
using maa.perf.test.core.Utils;

namespace maa.perf.test.core.Model
{
    //
    // Program options.  Options can be specified two ways:
    //   * via the the mixfile (one command line parameter)
    //   * via command line parameters (numerous command line parameters)
    //
    // The set of options available via command line parameters is
    // a subset of what's available via a mixfile.
    //
    public class Options
    {
        // Global
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages")]
        public bool Verbose { get; set; }

        // ******************************************************************
        // Option #1 - the mixfile
        // ******************************************************************

        [Option('x', "mixfile", Required = false, HelpText = "Mix file (JSON, defines mix of API calls)")]
        public string MixFileName { get; set; }

        // ******************************************************************
        // Option #2 - command line parameters
        // ******************************************************************

        // Orchestration info
        [Option('c', "connections", Required = false, HelpText = "Number of simultaneous connections (and calls) to the MAA service")]
        public long SimultaneousConnections { get; set; }

        [Option('r', "rps", Required = false, HelpText = "Target RPS")]
        public double TargetRPS { get; set; }

        [Option('f', "forcereconnects", Required = false, HelpText = "Force reconnects on each request")]
        public bool ForceReconnects { get; set; }

        [Option('m', "rampuptime", Required = false, HelpText = "Ramp up time in seconds")]
        public int RampUpTimeSeconds { get; set; }

        [Option('i', "testtime", Required = false, HelpText = "Testing time in seconds")]
        public int TestTimeSeconds { get; set; }

        [Option('q', "quote", Required = false, HelpText = "Enclave info file containing the SGX quote")]
        public string EnclaveInfoFile { get; set; }

        // API info
        [Option('a', "api", Required = false, HelpText = "REST Api to test: {AttestSgx, AttestOpenEnclave, GetOpenIdConfiguration, GetCerts, GetServiceHealth}")]
        public Api RestApi { get; set; }

        [Option('w', "previewapiversion", Required = false, HelpText = "Use preview api-version instead of GA")]
        public bool UsePreviewApiVersion { get; set; }

        [Option('o', "port", Required = false, HelpText = "Override service port number (default is 443)")]
        public string ServicePort { get; set; }

        [Option('h', "http", Required = false, HelpText = "Connect via HTTP (default is HTTPS)")]
        public bool UseHttp { get; set; }

        [Option('u', "url", Required = false, HelpText = "Load test a HTTP GET request for the provided URL")]
        public string Url { get; set; }

        // Provider info
        [Option('p', "provider", Required = false, HelpText = "Attestation provider DNS name")]
        public string AttestationProvider { get; set; }

        [Option('t', "tenant", Required = false, HelpText = "Override tenant name (default extracted from DNS name)")]
        public string TenantName { get; set; }

        [Option('z', "providercount", Required = false, HelpText = "Provider count (default = 1)")]
        public int ProviderCount { get; set; }

        // The following should always be accurate regardless of how command line parameters are set
        public MixInfo GetMixInfo()
        {
            var theMixInfo = default(MixInfo);

            if (!string.IsNullOrEmpty(MixFileName))
            {
                theMixInfo = MixInfo.ReadMixInfo(MixFileName);
            }
            else
            {
                theMixInfo = new MixInfo();

                theMixInfo.TestRuns.Add(new TestRunInfo()
                {
                    SimultaneousConnections = this.SimultaneousConnections,
                    TargetRPS = this.TargetRPS,
                    RampUpTimeSeconds = this.RampUpTimeSeconds,
                    TestTimeSeconds = this.TestTimeSeconds,
                    ForceReconnects = this.ForceReconnects,
                    EnclaveInfoFile = this.EnclaveInfoFile
                });

                theMixInfo.ApiMix.Add(new WeightedApiInfo()
                {
                    ApiName = this.RestApi,
                    UsePreviewApi = this.UsePreviewApiVersion,
                    ServicePort = this.ServicePort,
                    UseHttp = this.UseHttp,
                    Url = this.Url,
                    Weight = 100.0d,
                    Percentage = 1.0d
                });

                theMixInfo.ProviderMix.Add(new WeightedAttestationProvidersInfo()
                {
                    DnsName = this.AttestationProvider,
                    TenantNameOverride = this.TenantName,
                    ProviderCount = this.ProviderCount,
                    Weight = 100.0d,
                    Percentage = 1.0d
                });
            }

            return theMixInfo;
        }

        public Options()
        {
            Verbose = false;

            MixFileName = null;

            SimultaneousConnections = 5;
            TargetRPS = 1;
            ForceReconnects = false;
            RampUpTimeSeconds = 0;
            TestTimeSeconds = int.MaxValue;
            EnclaveInfoFile = "./Quotes/enclave.info.release.json";

            RestApi = Api.None;
            UsePreviewApiVersion = false;
            ServicePort = "443";
            UseHttp = false;
            Url = null;

            AttestationProvider = "sharedcac.cac.attest.azure.net";
            TenantName = null;
            ProviderCount = 1;
        }

        public void Trace()
        {
            Tracer.TraceInfo($"");
            Tracer.TraceInfo($"Mix File Name            : {MixFileName}");

            Tracer.TraceInfo($"");
            Tracer.TraceInfo($"**** Orchestration info");
            Tracer.TraceInfo($"Simultaneous Connections : {SimultaneousConnections}");
            Tracer.TraceInfo($"Target RPS               : {TargetRPS}");
            Tracer.TraceInfo($"Force Reconnects         : {ForceReconnects}");
            Tracer.TraceInfo($"RampUpTimeSeconds        : {RampUpTimeSeconds}");
            Tracer.TraceInfo($"Test time in seconds     : {TestTimeSeconds}");
            Tracer.TraceInfo($"Enclave Info File        : {EnclaveInfoFile}");

            Tracer.TraceInfo($"");
            Tracer.TraceInfo($"**** API info");
            Tracer.TraceInfo($"REST Api                 : {RestApi}");
            Tracer.TraceInfo($"Use Preview API Version  : {UsePreviewApiVersion}");
            Tracer.TraceInfo($"Service port             : {ServicePort}");
            Tracer.TraceInfo($"Use HTTP                 : {UseHttp}");
            Tracer.TraceInfo($"Url                      : {Url}");

            Tracer.TraceInfo($"");
            Tracer.TraceInfo($"**** Provider info");
            Tracer.TraceInfo($"Attestation Provider     : {AttestationProvider}");
            Tracer.TraceInfo($"Tenant Name Override     : {TenantName}");
            Tracer.TraceInfo($"ProviderCount            : {ProviderCount}");
            Tracer.TraceInfo($"");
        }
    }
}