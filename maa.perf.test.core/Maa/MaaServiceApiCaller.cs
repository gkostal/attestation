using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using maa.perf.test.core.Model;
using maa.perf.test.core.Utils;

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
            var randomProvidersDescription = _weightedProviders.GetRandomWeightedSample();
            var individualProviders = randomProvidersDescription.GetAttestationProviders();
            var randomSpecificProvider = individualProviders.GetRandomSample();

            var maaConnectionInfo = new MaaConnectionInfo()
            {
                DnsName = randomSpecificProvider.DnsName,
                TenantNameOverride = randomSpecificProvider.TenantNameOverride,
                ForceReconnects = _forceReconnects,
                ServicePort = _apiInfo.ServicePort,
                UseHttp = _apiInfo.UseHttp
            };

            var maaService = new MaaService(maaConnectionInfo);
            return GetCallback(_apiInfo)(maaService);
        }

        private async Task<double> CallAttestSgxPreviewApiVersionAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestOpenEnclaveAsync(new Maa.Preview.AttestOpenEnclaveRequestBody(_enclaveInfo)));
        }

        private async Task<double> CallAttestSgxGaApiVersionAsync(MaaService maaService)
        {
            return await WrapServiceCallAsync(async () => await maaService.AttestOpenEnclaveAsync(new Maa.Ga.AttestOpenEnclaveRequestBody(_enclaveInfo)));
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

        private Func<MaaService, Task<double>> GetCallback(ApiInfo theApiInfo)
        {
            switch (theApiInfo.ApiName)
            {
                case Api.AttestOpenEnclave:
                    if (theApiInfo.UsePreviewApi)
                        return CallAttestSgxPreviewApiVersionAsync;
                    else
                        return CallAttestSgxGaApiVersionAsync;
                case Api.AttestSgx:
                    return CallAttestSgxGaApiVersionAsync;
                case Api.GetCerts:
                    return GetCertsAsync;
                case Api.GetOpenIdConfiguration:
                    return GetOpenIdConfigurationAsync;
                case Api.GetServiceHealth:
                    return GetServiceHealthAsync;
                default:
                    return CallAttestSgxGaApiVersionAsync;
            }
        }
    }
}
