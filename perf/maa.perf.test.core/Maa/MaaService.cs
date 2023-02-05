using maa.perf.test.core.Authentication;
using maa.perf.test.core.Model;
using maa.perf.test.core.Utils;
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
        public class MaaResponse
        {
            public string Body { get; }
            public PerformanceInformation PerfInfo { get; }

            public MaaResponse()
            {
                Body = string.Empty;
                PerfInfo = new PerformanceInformation();
            }

            public MaaResponse(string body, PerformanceInformation perfInfo)
            {
                Body = body;
                PerfInfo = perfInfo;
            }
        }

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
            //Tracer.TraceVerbose($"MaaService constructor - force reconnect flag == {_connectionInfo.ForceReconnects}");
        }

        #region api-version both
        public async Task<MaaResponse> GetOpenIdConfigurationAsync(ApiVersion apiVersion)
        {
            return await DoGetAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/.well-known/openid-configuration?api-version={apiVersion.Resolve()}");
        }
        public async Task<MaaResponse> GetCertsAsync(ApiVersion apiVersion)
        {
            return await DoGetAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/certs?api-version={apiVersion.Resolve()}");
        }
        public async Task<MaaResponse> GetServiceHealthAsync(ApiVersion apiVersion)
        {
            return await DoGetAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/servicehealth?api-version={apiVersion.Resolve()}");
        }
        #endregion

        #region api-version 2018-09-01-preview
        public async Task<MaaResponse> AttestSgxEnclaveAsync(Preview.AttestSgxEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/attest/SgxEnclave?api-version=2018-09-01-preview", requestBody);
        }
        public async Task<MaaResponse> AttestVsmEnclaveAsync()
        {
            await Task.Run(() => throw new NotImplementedException($"{GetMyName()}"));
            return default(MaaResponse);
        }
        public async Task<MaaResponse> AttestVbsEnclaveAsync()
        {
            await Task.Run(() => throw new NotImplementedException($"{GetMyName()}"));
            return default(MaaResponse);
        }
        public async Task<MaaResponse> AttestTeeSgxEnclaveAsync(Preview.AttestTeeSgxEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/attest/Tee/SgxEnclave?api-version=2018-09-01-preview", requestBody);
        }
        public async Task<MaaResponse> AttestTeeOpenEnclaveAsync(Preview.AttestTeeOpenEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/attest/Tee/OpenEnclave?api-version=2018-09-01-preview", requestBody);
        }
        public async Task<MaaResponse> AttestTeeVsmEnclaveAsync()
        {
            await Task.Run(() => throw new NotImplementedException($"{GetMyName()}"));
            return default(MaaResponse);
        }
        public async Task<MaaResponse> AttestTeeVbsEnclaveAsync()
        {
            await Task.Run(() => throw new NotImplementedException($"{GetMyName()}"));
            return default(MaaResponse);
        }
        #endregion

        #region api-version 2020-10-01
        public async Task<MaaResponse> AttestSgxEnclaveAsync(Ga.AttestSgxEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/attest/SgxEnclave?api-version=2020-10-01", requestBody);
        }
        public async Task<MaaResponse> AttestOpenEnclaveAsync(Ga.AttestOpenEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/attest/OpenEnclave?api-version=2020-10-01", requestBody);
        }
        public async Task<MaaResponse> AttestSevSnpVmAsync(Ga.AttestSevSnpRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.DnsName}:{_servicePort}/attest/SevSnpVm?api-version=2020-10-01", requestBody);
        }
        public async Task<MaaResponse> AttestTpmAsync()
        {
            await Task.Run(() => throw new NotImplementedException($"{GetMyName()}"));
            return default(MaaResponse);
        }
        #endregion

        public async Task<MaaResponse> GetUrlAsync(string url)
        {
            return await DoGetAsync(url);
        }

        private async Task<MaaResponse> DoGetAsync(string uri, [CallerMemberName] string caller = null)
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
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"{caller}: MAA service status code {(int)response.StatusCode}.  Details: '{errorBody}'");
            }

            // Return result
            var body = await response.Content.ReadAsStringAsync();
            var perfInfo = ParsePerformanceResponseHeader(response);
            return new MaaResponse(body, perfInfo);
        }

        private async Task<MaaResponse> DoPostAsync(string uri, object bodyObject)
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
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"AttestOpenEnclaveAsync: MAA service status code {(int)response.StatusCode}.  Details: '{errorBody}'");
            }

            // Return result
            var body = (await response.Content.ReadAsStringAsync()).Trim('"');
            var perfInfo = ParsePerformanceResponseHeader(response);
            return new MaaResponse(body, perfInfo);
        }
        private PerformanceInformation ParsePerformanceResponseHeader(HttpResponseMessage response)
        {
            var perfInfo =  new PerformanceInformation();

            if (response.Headers.TryGetValues("x-ms-maa-perf-info", out var values))
            {
                var iterator = values.GetEnumerator();
                iterator.MoveNext();
                perfInfo = PerformanceInformation.CreateFromHeaderString(iterator.Current);
            }

            return perfInfo;
        }

        private static string GetMyName([CallerMemberName] string me = null)
        {
            return me;
        }
    }
}