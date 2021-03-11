using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maa.perf.test.core.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AttestationProviderInfo
    {
        public AttestationProviderInfo()
        {
            DnsName = "";
            TenantNameOverride = "";
            ProviderCount = 1;
            Weight = 0.0d;
            Percentage = 0.0d;
        }

        [JsonProperty]
        public string DnsName { get; set; }
        [JsonProperty]
        public string TenantNameOverride { get; set; }
        [JsonProperty]
        public int ProviderCount { get; set; }
        [JsonProperty]
        public double Weight { get; set; }

        public double Percentage { get; set; }
    }
}
