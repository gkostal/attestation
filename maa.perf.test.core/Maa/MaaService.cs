using System;
using System.Net.Http;
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

        public HttpClient MyHttpClient =>
            forceReconnects ? new HttpClient(new AuthenticationDelegatingHandler()) : theHttpClient;

        public bool ForceReconnects => forceReconnects;

        static MaaService()
        {
            theHttpClient = new HttpClient(new AuthenticationDelegatingHandler());
        }

        public MaaService(string providerDnsName, bool forceReconnects)
        {
            this.providerDnsName = providerDnsName;
            this.forceReconnects = forceReconnects;
        }

        public async Task<string> AttestOpenEnclaveAsync(AttestOpenEnclaveRequestBody requestBody)
        {
            // Build request
            var uri = $"https://{providerDnsName}:443/attest/Tee/OpenEnclave?api-version=2018-09-01-preview";
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(JsonConvert.SerializeObject(requestBody));

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