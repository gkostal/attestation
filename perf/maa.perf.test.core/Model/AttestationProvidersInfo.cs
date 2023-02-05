namespace maa.perf.test.core.Model
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    [JsonObject(MemberSerialization.OptIn)]
    public class AttestationProvidersInfo : AttestationProviderInfo
    {
        private List<AttestationProviderInfo> expandedProviders;

        public AttestationProvidersInfo()
        {
            ProviderCount = 1;
        }

        [JsonProperty]
        public int ProviderCount { get; set; }

        public List<AttestationProviderInfo> GetAttestationProviders()
        {
            if (expandedProviders == null)
            {
                List<AttestationProviderInfo> providers = new List<AttestationProviderInfo>();

                if (ProviderCount > 1)
                {
                    var (dnsNameBase, dnsSubDomain, tenantNameBase) = ExtractBaseNames();
                    for (int i = 0; i < ProviderCount; i++)
                    {
                        providers.Add(CreateRangedProviderInfo(i, dnsNameBase, dnsSubDomain, tenantNameBase));
                    }
                }
                else
                {
                    providers.Add(new AttestationProviderInfo()
                    {
                        DnsName = DnsName,
                        IpAddress = IpAddress,
                        TenantNameOverride = TenantNameOverride
                    });
                }

                expandedProviders = providers;
            }

            return expandedProviders;
        }

        private AttestationProviderInfo CreateRangedProviderInfo(int index, string dnsNameBase, string dnsSubDomain, string tenantNameBase)
        {
            var dnsName = string.IsNullOrEmpty(dnsNameBase) ? DnsName : $"{dnsNameBase}{index}{dnsSubDomain}";
            var tenantNameOverride = string.IsNullOrEmpty(tenantNameBase) ? TenantNameOverride : $"{tenantNameBase}{index}";

            return new AttestationProviderInfo()
            {
                DnsName = dnsName,
                TenantNameOverride = tenantNameOverride
            };
        }
    }
}
