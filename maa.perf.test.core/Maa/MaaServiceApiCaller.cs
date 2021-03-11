using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using maa.perf.test.core.Model;

namespace maa.perf.test.core.Maa
{
    public class MaaServiceApiCaller
    {
        private ApiInfo _apiInfo;
        private List<AttestationProviderInfo> _providers;

        public MaaServiceApiCaller(ApiInfo apiInfo, List<AttestationProviderInfo> providers)
        {
            _apiInfo = apiInfo;
            _providers = providers;
        }

        public Task<double> CallApi()
        {
            return Task.FromResult(0.0d);
        }
    }
}
