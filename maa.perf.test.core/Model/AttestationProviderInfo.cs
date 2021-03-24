using Newtonsoft.Json;

namespace maa.perf.test.core.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AttestationProviderInfo
    {
        public AttestationProviderInfo()
        {
            DnsName = "";
            HostName = "";
            TenantNameOverride = "";
        }

        [JsonProperty]
        public string DnsName { get; set; }
        [JsonProperty]
        public string HostName { get; set; }
        [JsonProperty]
        public string TenantNameOverride { get; set; }
    }
}