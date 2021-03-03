using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using maa.perf.test.core.Authentication;
using maa.perf.test.core.Utils;
using Newtonsoft.Json;

namespace maa.perf.test.core.Maa
{
    public class MaaService
    {
        const string TenantNameRegEx = @"(\D*)(\d*)";
        const string ProviderDnsNameRegEx = @"(\D*)(\d*)\..*";
        private static HttpClient theHttpClient;
        private Options options;
        private string providerDnsName;
        private bool forceReconnects;
        private string servicePortNumber;
        private string tenantNameOverride;
        private string uriScheme;
        private string providerDnsNameBase;
        private string tenantNameOverrideBase;
        private int currentProviderCount;

        public HttpClient MyHttpClient =>
            forceReconnects ? new HttpClient(GetHttpRequestHandler()) : theHttpClient;

        static MaaService()
        {
            theHttpClient = new HttpClient(GetHttpRequestHandler());
        }

        public static DelegatingHandler GetHttpRequestHandler()
        {
            return new FailureInjectionDelegatingHandler(
                new AuthenticationDelegatingHandler(),
                new List<Tuple<string, string>>()
                {
                    //new Tuple<string, string>("Report", "Quote"), 
                });
        }

        public MaaService(Options optionsReference)
        {
            this.options = optionsReference;
            this.providerDnsName = optionsReference.AttestationProvider;
            this.forceReconnects = optionsReference.ForceReconnects;
            this.servicePortNumber = optionsReference.ServicePort;
            this.tenantNameOverride = optionsReference.TenantName;
            this.uriScheme = optionsReference.UseHttp ? "http" : "https";

            this.providerDnsNameBase = "";
            this.tenantNameOverrideBase = "";
            this.currentProviderCount = 0;
            if (optionsReference.ProviderCount > 1)
            {
                if (!this.providerDnsName.Equals("localhost", StringComparison.InvariantCultureIgnoreCase))
                {
                    var pre = Regex.Match(this.providerDnsName, ProviderDnsNameRegEx);
                    this.providerDnsNameBase = pre.Groups[1].Value;
                }
                if (!string.IsNullOrEmpty(this.tenantNameOverride))
                {
                    var tre = Regex.Match(this.tenantNameOverride, TenantNameRegEx);
                    this.tenantNameOverrideBase = tre.Groups[1].Value;
                }
            }
        }

        public async Task<string> AttestOpenEnclaveAsync(Preview.AttestOpenEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{uriScheme}://{GetDnsName()}:{servicePortNumber}/attest/Tee/OpenEnclave?api-version=2018-09-01-preview", requestBody);
        }

        //2020-10-01
        public async Task<string> AttestOpenEnclaveAsync(Ga.AttestOpenEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{uriScheme}://{GetDnsName()}:{servicePortNumber}/attest/OpenEnclave?api-version=2020-10-01", requestBody);
        }

        public async Task<string> GetOpenIdConfigurationAsync()
        {
            return await DoGetAsync($"{uriScheme}://{GetDnsName()}:{servicePortNumber}/.well-known/openid-configuration?api-version=2020-10-01");
        }

        public async Task<string> GetCertsAsync()
        {
            return await DoGetAsync($"{uriScheme}://{GetDnsName()}:{servicePortNumber}/certs?api-version=2020-10-01");
        }

        public async Task<string> GetServiceHealthAsync()
        {
            return await DoGetAsync($"{uriScheme}://{GetDnsName()}:{servicePortNumber}/servicehealth?api-version=2020-10-01");
        }

        public async Task<string> GetUrlAsync(string url)
        {
            return await DoGetAsync(url);
        }

        private string GetDnsName()
        {
            if (string.IsNullOrEmpty(providerDnsNameBase))
            {
                return providerDnsName;
            }
            else
            {
                if (currentProviderCount >= options.ProviderCount)
                {
                    currentProviderCount = 0;
                }
                return $"{providerDnsNameBase}{currentProviderCount++}";
            }
        }

        private string GetTenantNameOverride()
        {
            if (string.IsNullOrEmpty(tenantNameOverrideBase))
            {
                return tenantNameOverride;
            }
            else
            {
                if (currentProviderCount >= options.ProviderCount)
                {
                    currentProviderCount = 0;
                }
                return $"{tenantNameOverrideBase}{currentProviderCount++}";
            }
        }

        private async Task<string> DoGetAsync(string uri, [CallerMemberName] string caller = null)
        {
            // Build request
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Send request
            var response = await MyHttpClient.SendAsync(request);

            // Analyze failures
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"{caller}: MAA service status code {(int)response.StatusCode}.  Details: '{body}'");
            }

            // Return result
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> DoPostAsync(string uri, object bodyObject)
        {
            // Build request
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(JsonConvert.SerializeObject(bodyObject), null, "application/json");

            // Add tenant name override header if requested
            var tenantNameOverrideValue = GetTenantNameOverride();
            if (!string.IsNullOrEmpty(tenantNameOverrideValue))
            {
                request.Headers.Add("tenantName", tenantNameOverrideValue);
            }

            // Send request
            Tracer.TraceVerbose($"DoPostAsync: {tenantNameOverrideValue, -24} {uri}");
            var response = await MyHttpClient.SendAsync(request);

            // Analyze failures
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"AttestOpenEnclaveAsync: MAA service status code {(int)response.StatusCode}.  Details: '{body}'");
            }

            // Return result
            return (await response.Content.ReadAsStringAsync()).Trim('"');
        }
    }
}