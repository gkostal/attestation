namespace maa.perf.test.core.Maa
{
    using maa.perf.test.core.Authentication;
    using maa.perf.test.core.Model;
    using maa.perf.test.core.Utils;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class needs to be thread safe
    /// </summary>
    public class MaaService
    {
        private static ConcurrentDictionary<string, string> ParsedFiles = new ConcurrentDictionary<string, string>();

        private const string TenantNameHeder = "tenantName";
        private static HttpClient theHttpClient;
        private MaaConnectionInfo _connectionInfo;
        private string _uriScheme;
        private string _servicePort;
        private CancellationToken _cancellationToken;
        private static Dictionary<string, string> SgxPolicyForDnsName = new Dictionary<string, string>();
        private static SemaphoreSlim SgxPolicySemaphore = new SemaphoreSlim(1);

        public string TenantName => _connectionInfo.GetTenantName();

        public HttpClient MyHttpClient =>
            _connectionInfo.ForceReconnects ? CreateInitializedHttpClient() : theHttpClient;

        static MaaService()
        {
            theHttpClient = CreateInitializedHttpClient();

            // Every minute start with a new set of connections, allowing the client to spread
            // load across a growing number of nodes in the cluster
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(60));
                        theHttpClient = new HttpClient(GetHttpRequestHandler());
                    }
                    catch (Exception x)
                    {
                        Tracer.TraceError($"MaaService HTTP client refresh thread ignoring exception: {x}");
                    }
                }
            });
        }

        private static DelegatingHandler GetHttpRequestHandler()
        {
            return new AuthenticationDelegatingHandler();
        }

        private static HttpClient CreateInitializedHttpClient()
        {
            var initializedHttpClient = new HttpClient(GetHttpRequestHandler());
            initializedHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MaaInfo/1.0");
            return initializedHttpClient;
        }

        public MaaService(MaaConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            _connectionInfo = connectionInfo;
            _uriScheme = _connectionInfo.UseHttp ? "http" : "https";
            _servicePort = string.IsNullOrEmpty(_connectionInfo.ServicePort) ? (_connectionInfo.UseHttp ? "80" : "443") : _connectionInfo.ServicePort;
            _cancellationToken = cancellationToken;
            //Tracer.TraceVerbose($"MaaService constructor - force reconnect flag == {_connectionInfo.ForceReconnects}");
        }

        #region api-version both
        public async Task<ServiceResponse> GetOpenIdConfigurationAsync(ApiVersion apiVersion)
        {
            return await DoGetAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/.well-known/openid-configuration?api-version={apiVersion.Resolve()}");
        }
        public async Task<ServiceResponse> GetCertsAsync(ApiVersion apiVersion)
        {
            return await DoGetAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/certs?api-version={apiVersion.Resolve()}");
        }
        public async Task<ServiceResponse> GetServiceHealthAsync(ApiVersion apiVersion)
        {
            return await DoGetAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/servicehealth?api-version={apiVersion.Resolve()}");
        }
        #endregion

        #region api-version 2018-09-01-preview
        public async Task<ServiceResponse> AttestSgxEnclaveAsync(Preview.AttestSgxEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/attest/SgxEnclave?api-version=2018-09-01-preview", requestBody);
        }
        public async Task<ServiceResponse> AttestTeeSgxEnclaveAsync(Preview.AttestTeeSgxEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/attest/Tee/SgxEnclave?api-version=2018-09-01-preview", requestBody);
        }
        public async Task<ServiceResponse> AttestTeeOpenEnclaveAsync(Preview.AttestTeeOpenEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/attest/Tee/OpenEnclave?api-version=2018-09-01-preview", requestBody);
        }
        public async Task<ServiceResponse> AttestTeeSevSnpVmAsync(Both.AttestSevSnpVmRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/attest/Tee/SevSnpVm?api-version=2018-09-01-preview", requestBody);
        }
        public async Task<ServiceResponse> AttestTeeAzureGuestAsync(Both.AttestAzureGuestRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/attest/Tee/AzureGuest?api-version=2018-09-01-preview", requestBody);
        }
        #endregion

        #region api-version 2020-10-01
        public async Task<ServiceResponse> AttestSgxEnclaveAsync(Ga.AttestSgxEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/attest/SgxEnclave?api-version=2020-10-01", requestBody);
        }
        public async Task<ServiceResponse> AttestOpenEnclaveAsync(Ga.AttestOpenEnclaveRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/attest/OpenEnclave?api-version=2020-10-01", requestBody);
        }
        public async Task<ServiceResponse> AttestSevSnpVmAsync(Both.AttestSevSnpVmRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/attest/SevSnpVm?api-version=2020-10-01", requestBody);
        }
        public async Task<ServiceResponse> AttestAzureGuestAsync(Both.AttestAzureGuestRequestBody requestBody)
        {
            return await DoPostAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/attest/AzureGuest?api-version=2020-10-01", requestBody);
        }
        public async Task<ServiceResponse> GetPolicyAsync()
        {
            return await DoGetAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/policies/SgxEnclave?api-version=2020-10-01");
        }
        public async Task<ServiceResponse> SetPolicyAsync()
        {
            var innerPolicyJwt = default(string);

            await SgxPolicySemaphore.WaitAsync();
            try
            {
                if (SgxPolicyForDnsName.ContainsKey(_connectionInfo.DnsName))
                {
                    innerPolicyJwt = SgxPolicyForDnsName[_connectionInfo.DnsName];
                }
                else
                {
                    var getPolicyResponse = await DoGetAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/policies/SgxEnclave?api-version=2020-10-01");
                    innerPolicyJwt = JoseHelper.ExtractJosePartField(getPolicyResponse.Body, 1, "x-ms-policy").ToString();
                    SgxPolicyForDnsName[_connectionInfo.DnsName] = innerPolicyJwt;
                }
            }
            finally
            {
                SgxPolicySemaphore.Release();
            }

            return await DoPutAsync($"{_uriScheme}://{_connectionInfo.GetUrlSchemeName()}:{_servicePort}/policies/SgxEnclave?api-version=2020-10-01", innerPolicyJwt, false);
        }
        #endregion

        public async Task<ServiceResponse> GetUrlAsync(string url)
        {
            return await DoGetAsync(url);
        }

        public async Task<ServiceResponse> PostUrlAsync(string url, string jsonFileName)
        {
            var content = new StringContent($"{GetFileJsonObjectValueString(jsonFileName)}", null, "application/json");
            return await DoPostOrPutAsync(true, url, content);
        }

        public async Task<ServiceResponse> PutUrlAsync(string url, string jsonFileName)
        {
            var content = new StringContent($"{GetFileJsonObjectValueString(jsonFileName)}", null, "application/json");
            return await DoPostOrPutAsync(false, url, content);
        }

        private string GetFileJsonObjectValueString(string jsonFileName)
        {
            if (!ParsedFiles.ContainsKey(jsonFileName))
            {
                var jsonString = File.ReadAllText(jsonFileName);

                // Deserialize and then serialize to ensure well formed JSON (throw an exception othwerwise)
                var jsonObject = JsonConvert.DeserializeObject(jsonString);
                ParsedFiles[jsonFileName] = JsonConvert.SerializeObject(jsonObject);
            }

            return ParsedFiles[jsonFileName];
        }

        private async Task<ServiceResponse> DoGetAsync(string uri, [CallerMemberName] string caller = null)
        {
            // Build request
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Add tenant name override header if requested
            if (!string.IsNullOrEmpty(_connectionInfo.TenantNameOverride))
            {
                request.Headers.Add(TenantNameHeder, _connectionInfo.TenantNameOverride);
            }

            // Specify host header as DNS name if using an IP Address
            if (!string.IsNullOrEmpty(_connectionInfo.IpAddress))
            {
                request.Headers.Host = _connectionInfo.DnsName;
            }

            // Add additional headers
            foreach (var h in _connectionInfo.AdditionalHeaders)
            {
                request.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            // Send request
            Tracer.TraceVerbose($"HTTP GET: [{uri}, {request.Headers.Host}, {_connectionInfo.TenantNameOverride}]");
            var response = await MyHttpClient.SendAsync(request, _cancellationToken);

            // 200 and 429 are well understood response codes
            if ((response.StatusCode == System.Net.HttpStatusCode.OK) || (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests))
            {
                // Return result
                var body = await response.Content.ReadAsStringAsync();
                var perfInfo = ParsePerformanceResponseHeader(response);
                var serviceVersion = ParseServiceVersionResponseHeader(response);
                return new ServiceResponse((int)response.StatusCode, body, perfInfo, serviceVersion);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"{caller}: MAA service status code {(int)response.StatusCode}.  Details: '{errorBody}'");
            }
        }

        private async Task<ServiceResponse> DoPostAsync(string uri, object bodyObject, [CallerMemberName] string caller = null)
        {
            return await DoPostOrPutAsync(true, uri, new StringContent(JsonConvert.SerializeObject(bodyObject), null, "application/json"), caller);
        }

        private async Task<ServiceResponse> DoPutAsync(string uri, object bodyObject, bool contentTypeIsJson, [CallerMemberName] string caller = null)
        {
            return await DoPostOrPutAsync(false, uri, new StringContent(JsonConvert.SerializeObject(bodyObject), null, contentTypeIsJson ? "application/json" : "text/plain"), caller);
        }

        private async Task<ServiceResponse> DoPostOrPutAsync(bool isPost, string uri, HttpContent content, [CallerMemberName] string caller = null)
        {
            // Build request
            var request = new HttpRequestMessage(isPost ? HttpMethod.Post : HttpMethod.Put, uri);
            request.Content = content;

            // Add tenant name override header if requested
            if (!string.IsNullOrEmpty(_connectionInfo.TenantNameOverride))
            {
                request.Headers.Add(TenantNameHeder, _connectionInfo.TenantNameOverride);
            }

            // Specify host header as DNS name if using an IP Address
            if (!string.IsNullOrEmpty(_connectionInfo.IpAddress))
            {
                request.Headers.Host = _connectionInfo.DnsName;
            }

            // Add additional headers
            foreach (var h in _connectionInfo.AdditionalHeaders)
            {
                request.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            // Send request
            var httpVerb = isPost ? "POST" : "PUT";
            Tracer.TraceVerbose($"HTTP {httpVerb}: [{uri}, {request.Headers.Host}, {_connectionInfo.TenantNameOverride}]");
            var response = await MyHttpClient.SendAsync(request, _cancellationToken);

            // 200 and 429 are well understood response codes
            if ((response.StatusCode == System.Net.HttpStatusCode.OK) || (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests))
            {
                // Return result
                var body = (await response.Content.ReadAsStringAsync()).Trim('"');
                var perfInfo = ParsePerformanceResponseHeader(response);
                var serviceVersion = ParseServiceVersionResponseHeader(response);
                return new ServiceResponse((int)response.StatusCode, body, perfInfo, serviceVersion);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"{caller}: MAA service status code {(int)response.StatusCode}.  Details: '{errorBody}'");
            }
        }

        private PerformanceInformation ParsePerformanceResponseHeader(HttpResponseMessage response)
        {
            var perfInfo = new PerformanceInformation();

            if (response.Headers.TryGetValues("x-ms-maa-perf-info", out var values))
            {
                var iterator = values.GetEnumerator();
                iterator.MoveNext();
                perfInfo = PerformanceInformation.CreateFromHeaderString(iterator.Current);
            }

            return perfInfo;
        }

        private string ParseServiceVersionResponseHeader(HttpResponseMessage response)
        {
            string serviceVersion = string.Empty;

            if (response.Headers.TryGetValues("x-ms-maa-service-version", out var values))
            {
                var iterator = values.GetEnumerator();
                iterator.MoveNext();
                serviceVersion = iterator.Current;
            }

            return serviceVersion;
        }

        private static string GetMyName([CallerMemberName] string me = null)
        {
            return me;
        }
    }
}