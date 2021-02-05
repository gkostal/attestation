using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using maa.perf.test.core.Authentication;
using Newtonsoft.Json;

namespace maa.perf.test.core.Maa
{
    public class MaaService
    {
        private static HttpClient theHttpClient;
        private string providerDnsName;
        private bool forceReconnects;
        private string servicePortNumber;
        private string tenantNameOverride;
        private string uriScheme;

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

        public MaaService(string providerDnsName, bool forceReconnects, string servicePortNumber, string tenantNameOverride, bool useHttp)
        {
            this.providerDnsName = providerDnsName;
            this.forceReconnects = forceReconnects;
            this.servicePortNumber = servicePortNumber;
            this.tenantNameOverride = tenantNameOverride;
            this.uriScheme = useHttp ? "http" : "https";
        }

        public async Task<string> AttestOpenEnclaveAsync(Preview.AttestOpenEnclaveRequestBody requestBody)
        {
            return await DoAttestOpenEnclaveAsync($"{uriScheme}://{providerDnsName}:{servicePortNumber}/attest/Tee/OpenEnclave?api-version=2018-09-01-preview", requestBody);
        }

        //2020-10-01
        public async Task<string> AttestOpenEnclaveAsync(Ga.AttestOpenEnclaveRequestBody requestBody)
        {
            return await DoAttestOpenEnclaveAsync($"{uriScheme}://{providerDnsName}:{servicePortNumber}/attest/OpenEnclave?api-version=2020-10-01", requestBody);
        }

        public async Task<string> GetOpenIdConfigurationAsync()
        {
            return await DoGetAsync($"{uriScheme}://{providerDnsName}:{servicePortNumber}/.well-known/openid-configuration?api-version=2020-10-01");
        }

        public async Task<string> GetCertsAsync()
        {
            return await DoGetAsync($"{uriScheme}://{providerDnsName}:{servicePortNumber}/certs?api-version=2020-10-01");
        }
        public async Task<string> GetServiceHealthAsync()
        {
            return await DoGetAsync($"{uriScheme}://{providerDnsName}:{servicePortNumber}/servicehealth?api-version=2020-10-01");
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
            var jwt = await response.Content.ReadAsStringAsync();
            return jwt.Trim('"');
        }

        private async Task<string> DoAttestOpenEnclaveAsync(string uri, object bodyObject)
        {
            // Build request
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(JsonConvert.SerializeObject(bodyObject), null, "application/json");

            // Add tenant name override header if requested
            if (!string.IsNullOrEmpty(tenantNameOverride))
            {
                request.Headers.Add("tenantName", tenantNameOverride);
            }

            // Send request
            var response = await MyHttpClient.SendAsync(request);

            // Analyze failures
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"AttestOpenEnclaveAsync: MAA service status code {(int)response.StatusCode}.  Details: '{body}'");
            }

            // Return result
            var jwt = await response.Content.ReadAsStringAsync();
            return jwt.Trim('"');
        }
    }
}