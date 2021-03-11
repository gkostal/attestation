using maa.perf.test.core.Authentication;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace maa.perf.test.core.Maa
{
    /// <summary>
    /// This class needs to be thread safe
    /// </summary>
    public class MaaService
    {
        private const string TenantNameHeder = "tenantName";
        private static HttpClient theHttpClient;
        private MaaConnectionInfo _connectionInfo;
        private string _uriScheme;
        private string _servicePort;

        public HttpClient MyHttpClient =>
            _connectionInfo.ForceReconnects ? new HttpClient(GetHttpRequestHandler()) : theHttpClient;

        static MaaService()
        {
            theHttpClient = new HttpClient(GetHttpRequestHandler());
        }

        private static DelegatingHandler GetHttpRequestHandler()
        {
            return new FailureInjectionDelegatingHandler(
                new AuthenticationDelegatingHandler(),
                new List<Tuple<string, string>>()
                {
                    //new Tuple<string, string>("Report", "Quote"), 
                });
        }

        public MaaService(MaaConnectionInfo connectionInfo)
        {
            _connectionInfo = connectionInfo;
            _uriScheme = _connectionInfo.UseHttp ? "http" : "https";
            _servicePort = string.IsNullOrEmpty(_connectionInfo.ServicePort) ? (_connectionInfo.UseHttp ? "80" : "443") : _connectionInfo.ServicePort;

            /*
            const string TenantNameRegEx = @"(\D*)(\d*)";
            const string ProviderDnsNameRegEx = @"(\D*)(\d*)\..*";

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
            */
        }

        public async Task<string> AttestOpenEnclaveAsync(Preview.AttestOpenEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/attest/Tee/OpenEnclave?api-version=2018-09-01-preview", requestBody);
        }

        //2020-10-01
        public async Task<string> AttestOpenEnclaveAsync(Ga.AttestOpenEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/attest/OpenEnclave?api-version=2020-10-01", requestBody);
        }

        public async Task<string> GetOpenIdConfigurationAsync()
        {
            return await DoGetAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/.well-known/openid-configuration?api-version=2020-10-01");
        }

        public async Task<string> GetCertsAsync()
        {
            return await DoGetAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/certs?api-version=2020-10-01");
        }

        public async Task<string> GetServiceHealthAsync()
        {
            return await DoGetAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/servicehealth?api-version=2020-10-01");
        }

        public async Task<string> GetUrlAsync(string url)
        {
            return await DoGetAsync(url);
        }

        private async Task<string> DoGetAsync(string uri, [CallerMemberName] string caller = null)
        {
            // Build request
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Add tenant name override header if requested
            if (!string.IsNullOrEmpty(_connectionInfo.TenantNameOverride))
            {
                request.Headers.Add(TenantNameHeder, _connectionInfo.TenantNameOverride);
            }

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
            if (!string.IsNullOrEmpty(_connectionInfo.TenantNameOverride))
            {
                request.Headers.Add(TenantNameHeder, _connectionInfo.TenantNameOverride);
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
            return (await response.Content.ReadAsStringAsync()).Trim('"');
        }
    }
}