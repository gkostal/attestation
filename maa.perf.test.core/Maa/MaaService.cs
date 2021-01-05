using System;
using System.Collections.Generic;
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

        public MaaService(string providerDnsName, bool forceReconnects)
        {
            this.providerDnsName = providerDnsName;
            this.forceReconnects = forceReconnects;
        }

        public async Task<string> AttestOpenEnclaveAsync(Preview.AttestOpenEnclaveRequestBody requestBody)
        {
            return await DoAttestOpenEnclaveAsync($"https://{providerDnsName}:443/attest/Tee/OpenEnclave?api-version=2018-09-01-preview", requestBody);
        }

        //2020-10-01
        public async Task<string> AttestOpenEnclaveAsync(Ga.AttestOpenEnclaveRequestBody requestBody)
        {
            return await DoAttestOpenEnclaveAsync($"https://{providerDnsName}:443/attest/OpenEnclave?api-version=2020-10-01", requestBody);
        }

        private async Task<string> DoAttestOpenEnclaveAsync(string uri, object bodyObject)
        {
            // Build request
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(JsonConvert.SerializeObject(bodyObject), null, "application/json");

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