using maa.perf.test.core.Model;
using maa.perf.test.core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maa.perf.test.core.Maa
{
    public class MaaServiceApiCaller
    {
        private static Dictionary<string, long> _exceptionHistory = new Dictionary<string, long>();
        private ApiInfo _apiInfo;
        private List<WeightedAttestationProvidersInfo> _weightedProviders;
        private EnclaveInfo _enclaveInfo;
        private bool _forceReconnects;

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

        public MaaServiceApiCaller(ApiInfo apiInfo, List<WeightedAttestationProvidersInfo> weightedProviders, string enclaveInfoFileName, bool forceReconnects)
        {
            _apiInfo = apiInfo;
            _weightedProviders = weightedProviders;
            _enclaveInfo = EnclaveInfo.CreateFromFile(enclaveInfoFileName);
            _forceReconnects = forceReconnects;
        }

        public async Task<PerformanceInformation> CallApi()
        {
            var maaResponse = await GetCallback().Invoke(CreateMaaService());
            return maaResponse.PerfInfo;
        }

        private MaaService CreateMaaService()
        {
            MaaConnectionInfo maaConnectionInfo = null;

            if (string.IsNullOrEmpty(_apiInfo.Url))
            {
                var randomProvidersDescription = _weightedProviders.GetRandomWeightedSample();
                var individualProviders = randomProvidersDescription.GetAttestationProviders();
                var randomSpecificProvider = individualProviders.GetRandomSample();

                maaConnectionInfo = new MaaConnectionInfo()
                {
                    DnsName = randomSpecificProvider.DnsName,
                    TenantNameOverride = randomSpecificProvider.TenantNameOverride,
                    ForceReconnects = _forceReconnects,
                    ServicePort = _apiInfo.ServicePort,
                    UseHttp = _apiInfo.UseHttp
                };
            }
            else
            {
                maaConnectionInfo = new MaaConnectionInfo()
                {
                    DnsName = string.Empty,
                    TenantNameOverride = string.Empty,
                    ForceReconnects = _forceReconnects,
                    ServicePort = _apiInfo.ServicePort,
                    UseHttp = _apiInfo.UseHttp
                };
            }

            return new MaaService(maaConnectionInfo);
        }

        private async Task<MaaService.MaaResponse> CallAttestOpenEnclavePreviewAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestOpenEnclaveAsync(new Maa.Preview.AttestOpenEnclaveRequestBody(_enclaveInfo)));
        }

        private async Task<MaaService.MaaResponse> CallAttestOpenEnclaveGaAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestOpenEnclaveAsync(new Maa.Ga.AttestOpenEnclaveRequestBody(_enclaveInfo)));
        }

        private async Task<MaaService.MaaResponse> CallAttestSgxEnclaveGaAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestSgxEnclaveAsync(new Maa.Ga.AttestSgxEnclaveRequestBody(_enclaveInfo)));
        }

        private async Task<MaaService.MaaResponse> GetCertsAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.GetCertsAsync());
        }

        private async Task<MaaService.MaaResponse> GetOpenIdConfigurationAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.GetOpenIdConfigurationAsync());
        }

        private async Task<MaaService.MaaResponse> GetServiceHealthAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.GetServiceHealthAsync());
        }

        private async Task<MaaService.MaaResponse> GetUrlAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.GetUrlAsync(_apiInfo.Url));
        }

        private async Task<MaaService.MaaResponse> WrapServiceCallAsync(Func<Task<MaaService.MaaResponse>> callServiceAsync)
        {
            try
            {
                var result = await callServiceAsync();
                return result;
            }
            // Intentionally swallow all exceptions here since we never want to cause
            // an exception accessing the MAA service to cause the AsyncFor task to 
            // stop scheduling new requests in the future.  IOW, if there are MAA 
            // failures, we note them and resiliently keep trying in the future.
            // "Note them" means:
            //   * we log the exception to the console
            //   * we report the exception summary when the application ends
            catch (Exception x)
            {
                Tracer.TraceError($"Exception caught: {x.ToString()}");
                lock (_exceptionHistory)
                {
                    if (_exceptionHistory.ContainsKey(x.Message))
                    {
                        _exceptionHistory[x.Message]++;
                    }
                    else
                    {
                        _exceptionHistory[x.Message] = 1;
                    }
                }
            }

            return await Task.FromResult(new MaaService.MaaResponse());
        }

        private Func<MaaService, Task<MaaService.MaaResponse>> GetCallback()
        {
            if (string.IsNullOrEmpty(_apiInfo.Url))
            {
                switch (_apiInfo.ApiName)
                {
                    case Api.AttestOpenEnclave:
                        if (_apiInfo.UsePreviewApi)
                            return CallAttestOpenEnclavePreviewAsync;
                        else
                            return CallAttestOpenEnclaveGaAsync;
                    case Api.AttestSgx:
                        return CallAttestSgxEnclaveGaAsync;
                    case Api.GetCerts:
                        return GetCertsAsync;
                    case Api.GetOpenIdConfiguration:
                        return GetOpenIdConfigurationAsync;
                    case Api.GetServiceHealth:
                        return GetServiceHealthAsync;
                    default:
                        return CallAttestOpenEnclaveGaAsync;
                }
            }
            else
            {
                return GetUrlAsync;
            }
        }
    }
}
