namespace maa.perf.test.core.Model
{
    using CommandLine;
    using maa.perf.test.core.Utils;
    using System;
    using System.Collections.Generic;

    //
    // Program options.  Options can be specified two ways:
    //   * via the the mixfile (one command line parameter)
    //   * via command line parameters (numerous command line parameters)
    //
    // The set of options available via command line parameters is
    // a subset of what's available via a mixfile.
    //
    // Example command lines:
    // --posturl http://localhost.:5000/test/posthelloasync/50 --postfile test\sevsnp.json -c 1 -r 1
    //
    public class Options
    {
        // Global
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose trace messages")]
        public bool Verbose { get; set; }

        [Option('l', "silent", Required = false, HelpText = "Set output to no trace messages")]
        public bool Silent { get; set; }

        [Option('n', "nongraceful", Required = false, HelpText = "Perform non-graceful shutdown (dropping network connections)")]
        public bool NonGracefulTermination { get; set; }
        public bool GracefulTermination => !NonGracefulTermination;

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

        [Option('s', "serversidetime", Required = false, HelpText = "Measure server side time if available")]
        public bool MeasureServerSideTime { get; set; }

        [Option('m', "rampuptime", Required = false, HelpText = "Ramp up time in seconds")]
        public int RampUpTimeSeconds { get; set; }

        [Option('y', "testtime", Required = false, HelpText = "Testing time in seconds")]
        public int TestTimeSeconds { get; set; }

        [Option('q', "quote", Required = false, HelpText = "Enclave info file containing the SGX quote")]
        public string EnclaveInfoFile { get; set; }

        // API info
        [Option('a', "api", Required = false, HelpText = "REST Api to test: \n{ \n%API%\n}")]
        public Api RestApi { get; set; }

        [Option('w', "previewapiversion", Required = false, HelpText = "Use preview api-version instead of GA")]
        public bool UsePreviewApiVersion { get; set; }

        [Option('o', "port", Required = false, HelpText = "Override service port number (default is 443)")]
        public string ServicePort { get; set; }

        [Option('h', "http", Required = false, HelpText = "Connect via HTTP (default is HTTPS)")]
        public bool UseHttp { get; set; }

        [Option('u', "url", Required = false, HelpText = "Load test a HTTP GET request for the provided URL")]
        public string Url { get; set; }

        [Option('e', "headers", Required = false, HelpText = "Request header values (e.g. \"Name1=Value1;Name2=Value2\")")]
        public string Headers { get; set; }

        [Option("posturl", Required = false, HelpText = "Load test a HTTP POST request for the provided URL")]
        public string PostUrl { get; set; }

        [Option("postfile", Required = false, HelpText = "JSON request body payload for HTTP POST request")]
        public string PostFileName { get; set; }

        [Option("puturl", Required = false, HelpText = "Load test a HTTP PUT request for the provided URL")]
        public string PutUrl { get; set; }

        [Option("putfile", Required = false, HelpText = "JSON request body payload for HTTP PUT request")]
        public string PutFileName { get; set; }

        // Provider info
        [Option('p', "provider", Required = false, HelpText = "Attestation provider DNS name")]
        public string AttestationProvider { get; set; }

        [Option('i', "ipaddress", Required = false, HelpText = "Attestation provider IP Address (overrides DNS name resolution)")]
        public string IpAddress { get; set; }

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
                    MeasureServerSideTime = this.MeasureServerSideTime,
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
                    Headers = this.Headers,
                    PostUrl = this.PostUrl,
                    PostFile =  this.PostFileName,
                    PutUrl = this.PutUrl,
                    PutFile = this.PutFileName,
                    Weight = 100.0d,
                    Percentage = 1.0d
                });

                theMixInfo.ProviderMix.Add(new WeightedAttestationProvidersInfo()
                {
                    DnsName = this.AttestationProvider,
                    IpAddress = this.IpAddress,
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
            Silent = false;
            NonGracefulTermination = false;

            MixFileName = null;

            SimultaneousConnections = 1;
            TargetRPS = 1;
            ForceReconnects = false;
            MeasureServerSideTime = false;
            RampUpTimeSeconds = 0;
            TestTimeSeconds = int.MaxValue;
            EnclaveInfoFile = "./Quotes/enclave.info.release.json";

            RestApi = Api.None;
            UsePreviewApiVersion = false;
            ServicePort = "443";
            UseHttp = false;
            Url = null;
            Headers = null;

            AttestationProvider = "sharedcac.cac.attest.azure.net";
            TenantName = null;
            ProviderCount = 1;
        }

        public void Trace()
        {
            Tracer.TraceVerbose($"");
            Tracer.TraceVerbose($"Verbose tracing          : {Verbose}");
            Tracer.TraceVerbose($"Verbose silent           : {Silent}");
            Tracer.TraceVerbose($"Graceful Termination     : {GracefulTermination}");
            Tracer.TraceVerbose($"");
            Tracer.TraceVerbose($"Mix File Name            : {MixFileName}");

            Tracer.TraceVerbose($"");
            Tracer.TraceVerbose($"**** Orchestration info");
            Tracer.TraceVerbose($"Simultaneous Connections : {SimultaneousConnections}");
            Tracer.TraceVerbose($"Target RPS               : {TargetRPS}");
            Tracer.TraceVerbose($"Force Reconnects         : {ForceReconnects}");
            Tracer.TraceVerbose($"Measure server side time : {MeasureServerSideTime}");
            Tracer.TraceVerbose($"RampUpTimeSeconds        : {RampUpTimeSeconds}");
            Tracer.TraceVerbose($"Test time in seconds     : {TestTimeSeconds}");
            Tracer.TraceVerbose($"Enclave Info File        : {EnclaveInfoFile}");

            Tracer.TraceVerbose($"");
            Tracer.TraceVerbose($"**** API info");
            Tracer.TraceVerbose($"REST Api                 : {RestApi}");
            Tracer.TraceVerbose($"Use Preview API Version  : {UsePreviewApiVersion}");
            Tracer.TraceVerbose($"Service port             : {ServicePort}");
            Tracer.TraceVerbose($"Use HTTP                 : {UseHttp}");
            Tracer.TraceVerbose($"Url                      : {Url}");
            Tracer.TraceVerbose($"Headers                  : {Headers}");

            Tracer.TraceVerbose($"");
            Tracer.TraceVerbose($"**** Provider info");
            Tracer.TraceVerbose($"Attestation Provider     : {AttestationProvider}");
            Tracer.TraceVerbose($"Tenant Name Override     : {TenantName}");
            Tracer.TraceVerbose($"ProviderCount            : {ProviderCount}");
            Tracer.TraceVerbose($"");
        }
    }
}