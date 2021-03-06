using maa.perf.test.core.Model;
using maa.perf.test.core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maa.perf.test.core.Maa
{
    public class MaaServiceApiCaller
    {
        private ApiInfo _apiInfo;
        private List<WeightedAttestationProvidersInfo> _weightedProviders;
        private EnclaveInfo _enclaveInfo;
        private bool _forceReconnects;

        public MaaServiceApiCaller(ApiInfo apiInfo, List<WeightedAttestationProvidersInfo> weightedProviders, string enclaveInfoFileName, bool forceReconnects)
        {
            _apiInfo = apiInfo;
            _weightedProviders = weightedProviders;
            _enclaveInfo = EnclaveInfo.CreateFromFile(enclaveInfoFileName);
            _forceReconnects = forceReconnects;
        }

        public Task<double> CallApi()
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

            var maaService = new MaaService(maaConnectionInfo);
            return GetCallback().Invoke(maaService);
        }

        private async Task<double> CallAttestOpenEnclavePreviewAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestOpenEnclaveAsync(new Maa.Preview.AttestOpenEnclaveRequestBody(_enclaveInfo)));
        }

        private async Task<double> CallAttestOpenEnclaveGaAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestOpenEnclaveAsync(new Maa.Ga.AttestOpenEnclaveRequestBody(_enclaveInfo)));
        }

        private async Task<double> CallAttestSgxEnclaveGaAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestSgxEnclaveAsync(new Maa.Ga.AttestSgxEnclaveRequestBody(_enclaveInfo)));
        }

        private async Task<double> GetCertsAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.GetCertsAsync());
        }

        private async Task<double> GetOpenIdConfigurationAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.GetOpenIdConfigurationAsync());
        }

        private async Task<double> GetServiceHealthAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.GetServiceHealthAsync());
        }

        private async Task<double> GetUrlAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.GetUrlAsync(_apiInfo.Url));
        }

        private async Task<double> WrapServiceCallAsync(Func<Task<string>> callServiceAsync)
        {
            try
            {
                await callServiceAsync();
            }
            catch (Exception x)
            {
                Tracer.TraceError($"Exception caught: {x.ToString()}");
            }

            return await Task.FromResult(0.0);
        }

        private Func<MaaService, Task<double>> GetCallback()
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
