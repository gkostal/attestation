namespace maa.perf.test.core.Maa
{
    using maa.perf.test.core.Model;
    using maa.perf.test.core.Utils;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class MaaServiceApiCaller
    {
        private int _azureGuestQuoteIndex = 0;
        private static Dictionary<string, long> _exceptionHistory = new Dictionary<string, long>();
        private Dictionary<Api, Func<MaaService, Task<ServiceResponse>>> _apiMapping;
        private ApiInfo _apiInfo;
        private List<WeightedAttestationProvidersInfo> _weightedProviders;
        private EnclaveInfo _enclaveInfo;
        private bool _forceReconnects;
        private Dictionary<string, string> _additionalHeaders;
        private CancellationToken _cancellationToken;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private Random rnd = new Random();

        public static void TraceExceptionHistory()
        {
            lock (_exceptionHistory)
            {
                if (_exceptionHistory.Count == 0)
                {
                    Tracer.TraceInfo("Exception Summary: No exceptions encountered");
                }
                else
                {
                    Tracer.TraceWarning($"Exception Summary: {_exceptionHistory.Count} different types of exceptions encountered!");
                    foreach (var x in _exceptionHistory)
                    {
                        Tracer.TraceWarning($"{x.Value,10} : {x.Key}");
                    }
                }
            }
        }

        public MaaServiceApiCaller(ApiInfo apiInfo, List<WeightedAttestationProvidersInfo> weightedProviders, string enclaveInfoFileName, bool forceReconnects, Dictionary<string, string> additionalHeaders, CancellationToken cancellationToken)
        {
            _apiInfo = apiInfo;
            _weightedProviders = weightedProviders;
            _enclaveInfo = EnclaveInfo.CreateFromFile(enclaveInfoFileName);
            _forceReconnects = forceReconnects;
            _additionalHeaders = additionalHeaders;
            _cancellationToken = cancellationToken;
            _apiMapping = new Dictionary<Api, Func<MaaService, Task<ServiceResponse>>>
            {
                {  Api.AttestSgxEnclave, AttestSgxEnclaveAsync },
                {  Api.AttestTeeSgxEnclave, AttestTeeSgxEnclaveAsync },
                {  Api.AttestTeeOpenEnclave, AttestTeeOpenEnclaveAsync },
                {  Api.AttestTeeSevSnpVm, AttestTeeSevSnpVmAsync },
                {  Api.AttestTeeAzureGuest, AttestTeeAzureGuestAsync },
                {  Api.AttestOpenEnclave, AttestOpenEnclaveAsync },
                {  Api.AttestSevSnpVm, AttestSevSnpVmAsync },
                {  Api.AttestSevSnpVmUvm, AttestSevSnpVmUvmAsync },
                {  Api.AttestAzureGuest, AttestAzureGuestAsync },
                {  Api.GetCerts, GetCertsAsync },
                {  Api.GetOpenIdConfiguration, GetOpenIdConfigurationAsync },
                {  Api.GetServiceHealth, GetServiceHealthAsync },
                {  Api.GetPolicy, GetPolicyAsync },
                {  Api.SetPolicy, SetPolicyAsync },
            };
        }

        public async Task<ServiceResponse> CallApi()
        {
            var maaResponse = await GetCallback().Invoke(CreateMaaService());
            Tracer.TraceVerbose($"MAA response: [{maaResponse.StatusCode}, {maaResponse.Body}]");
            return maaResponse;
        }

        #region api-version both
        private async Task<ServiceResponse> AttestSgxEnclaveAsync(MaaService maaService)
        {
            if (_apiInfo.UsePreviewApi)
            {
                return await WrapServiceCallAsync(async () => await maaService.AttestSgxEnclaveAsync(new Maa.Preview.AttestSgxEnclaveRequestBody(_enclaveInfo)));
            }
            else
            {
                return await WrapServiceCallAsync(async () => await maaService.AttestSgxEnclaveAsync(new Maa.Ga.AttestSgxEnclaveRequestBody(_enclaveInfo)));
            }
        }
        private async Task<ServiceResponse> GetCertsAsync(MaaService maaService)
        {
            ApiVersion apiVersion = _apiInfo.UsePreviewApi ? ApiVersion.Preview : ApiVersion.GA;
            return await WrapServiceCallAsync(async () => await maaService.GetCertsAsync(apiVersion));
        }
        private async Task<ServiceResponse> GetOpenIdConfigurationAsync(MaaService maaService)
        {
            ApiVersion apiVersion = _apiInfo.UsePreviewApi ? ApiVersion.Preview : ApiVersion.GA;
            return await WrapServiceCallAsync(async () => await maaService.GetOpenIdConfigurationAsync(apiVersion));
        }
        private async Task<ServiceResponse> GetServiceHealthAsync(MaaService maaService)
        {
            ApiVersion apiVersion = _apiInfo.UsePreviewApi ? ApiVersion.Preview : ApiVersion.GA;
            return await WrapServiceCallAsync(async () => await maaService.GetServiceHealthAsync(apiVersion));
        }
        private async Task<ServiceResponse> GetPolicyAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.GetPolicyAsync());
        }
        private async Task<ServiceResponse> SetPolicyAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.SetPolicyAsync());
        }
        #endregion

        #region api-version 2018-09-01-preview
        private async Task<ServiceResponse> AttestTeeSgxEnclaveAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestTeeSgxEnclaveAsync(new Maa.Preview.AttestTeeSgxEnclaveRequestBody(_enclaveInfo)));
        }
        private async Task<ServiceResponse> AttestTeeOpenEnclaveAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestTeeOpenEnclaveAsync(new Maa.Preview.AttestTeeOpenEnclaveRequestBody(_enclaveInfo)));
        }
        private async Task<ServiceResponse> AttestTeeSevSnpVmAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestTeeSevSnpVmAsync(GetAttestSevSnpVmRequestBody(isGaApiVersion: false)));
        }
        private async Task<ServiceResponse> AttestTeeAzureGuestAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestTeeAzureGuestAsync(GetAttestAzureGuestRequestBody()));
        }
        #endregion

        #region api-version 2020-10-01
        private async Task<ServiceResponse> AttestOpenEnclaveAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestOpenEnclaveAsync(new Maa.Ga.AttestOpenEnclaveRequestBody(_enclaveInfo)));
        }
        private async Task<ServiceResponse> AttestSevSnpVmAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestSevSnpVmAsync(GetAttestSevSnpVmRequestBody(isGaApiVersion: true)));
        }
        private async Task<ServiceResponse> AttestSevSnpVmUvmAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestSevSnpVmAsync(Maa.Both.AttestSevSnpVmRequestBody.CreateFromFile("./Quotes/sevsnpvmuvm.report.info.sample.json")));
        }
        private async Task<ServiceResponse> AttestAzureGuestAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestAzureGuestAsync(GetAttestAzureGuestRequestBody()));
        }
        #endregion

        #region helper methods
        private MaaService CreateMaaService()
        {
            MaaConnectionInfo maaConnectionInfo = null;

            if ((string.IsNullOrEmpty(_apiInfo.Url)) && (string.IsNullOrEmpty(_apiInfo.PostUrl)) && (string.IsNullOrEmpty(_apiInfo.PutUrl)))
            {
                var randomProvidersDescription = _weightedProviders.GetRandomWeightedSample();
                var individualProviders = randomProvidersDescription.GetAttestationProviders();
                var randomSpecificProvider = individualProviders.GetRandomSample();

                maaConnectionInfo = new MaaConnectionInfo()
                {
                    DnsName = randomSpecificProvider.DnsName,
                    IpAddress = randomSpecificProvider.IpAddress,
                    TenantNameOverride = randomSpecificProvider.TenantNameOverride,
                    ForceReconnects = _forceReconnects,
                    ServicePort = _apiInfo.ServicePort,
                    UseHttp = _apiInfo.UseHttp,
                    AdditionalHeaders = _additionalHeaders
                };
            }
            else
            {
                maaConnectionInfo = new MaaConnectionInfo()
                {
                    DnsName = string.Empty,
                    IpAddress = string.Empty,
                    TenantNameOverride = string.Empty,
                    ForceReconnects = _forceReconnects,
                    ServicePort = _apiInfo.ServicePort,
                    UseHttp = _apiInfo.UseHttp,
                    AdditionalHeaders = _additionalHeaders
                };
            }

            return new MaaService(maaConnectionInfo, _cancellationToken);
        }

        private async Task<ServiceResponse> GetUrlAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.GetUrlAsync(_apiInfo.Url));
        }

        private async Task<ServiceResponse> PostUrlAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.PostUrlAsync(_apiInfo.PostUrl, _apiInfo.PostFile));
        }

        private async Task<ServiceResponse> PutUrlAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.PutUrlAsync(_apiInfo.PutUrl, _apiInfo.PutFile));
        }

        private async Task<ServiceResponse> WrapServiceCallAsync(Func<Task<ServiceResponse>> callServiceAsync)
        {
            try
            {
                var result = await callServiceAsync();
                return result;
            }
            catch (TaskCanceledException)
            {
                //Tracer.TraceError($"WrapServiceCallAsync: TaskCanceledException caught.  Rethrowing! {tce.Message}");
                throw;
            }
            catch (OperationCanceledException)
            {
                //Tracer.TraceError($"WrapServiceCallAsync: OperationCanceledException caught.  Rethrowing! {oce.Message}");
                throw;
            }
            // Intentionally swallow all other exceptions here since we never want to cause
            // an exception accessing the MAA service to cause the AsyncFor task to 
            // stop scheduling new requests in the future.  IOW, if there are MAA 
            // failures, we note them and resiliently keep trying in the future.
            // "Note them" means:
            //   * we log the exception to the console
            //   * we report the exception summary when the application ends
            catch (Exception x)
            {
                //Tracer.TraceError($"WrapServiceCallAsync: Exception caught: {x.ToString()}");
                var key = x.Message != null ? $"{x.GetType().FullName}: {x.Message}" : $"{x.GetType().FullName} : <null message>";
                lock (_exceptionHistory)
                {
                    if (_exceptionHistory.ContainsKey(key))
                    {
                        _exceptionHistory[key]++;
                    }
                    else
                    {
                        _exceptionHistory[key] = 1;
                    }
                }
            }

            return await Task.FromResult(new ServiceResponse());
        }

        private Func<MaaService, Task<ServiceResponse>> GetCallback()
        {
            if ((string.IsNullOrEmpty(_apiInfo.Url)) && (string.IsNullOrEmpty(_apiInfo.PostUrl)) && (string.IsNullOrEmpty(_apiInfo.PutUrl)))
            {
                lock (_apiMapping)
                {
                    return _apiMapping[_apiInfo.ApiName];
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(_apiInfo.Url))
                {
                    return GetUrlAsync;
                }
                else if (!string.IsNullOrEmpty(_apiInfo.PostUrl))
                {
                    return PostUrlAsync;
                }
                else // if (!string.IsNullOrEmpty(_apiInfo.PutUrl))
                {
                    return PutUrlAsync;
                }
            }
        }

        private Maa.Both.AttestAzureGuestRequestBody GetAttestAzureGuestRequestBody()
        {
            _azureGuestQuoteIndex = (_azureGuestQuoteIndex + 1) % Quotes.AzureGuestQuotes.AllActiveQuotes.Length;
            return new Both.AttestAzureGuestRequestBody(Quotes.AzureGuestQuotes.AllActiveQuotes[_azureGuestQuoteIndex]);
        }

        private Maa.Both.AttestSevSnpVmRequestBody GetAttestSevSnpVmRequestBody(bool isGaApiVersion)
        {
            var body = default(Both.AttestSevSnpVmRequestBody);
            if (isGaApiVersion)
            {
                // Scale note: This does NOT make an outbound call to the THIM service for each request, so MAA RPS is not limited by the THIM service
                body = Maa.Both.AttestSevSnpVmRequestBody.CreateFromFile("./Quotes/sevsnpvm.report.info.sample.jsonreport.json");
            }
            else
            {
                // Scale note: This makes an outbound call to the THIM service for each request, so MAA RPS is limited
                body = Maa.Both.AttestSevSnpVmRequestBody.CreateFromFile("./Quotes/sevsnpvm.report.info.sample.json");
            }
            return body;
        }
        #endregion
    }
}
