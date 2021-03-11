using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace maa.perf.test.core.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AttestationProvidersInfo : AttestationProviderInfo
    {
        const string TenantNameRegEx = @"(\D*)(\d*)";
        const string ProviderDnsNameRegEx = @"(\D*)(\d*)\..*";

        public AttestationProvidersInfo()
        {
            ProviderCount = 1;
        }

        [JsonProperty]
        public int ProviderCount { get; set; }

        public List<AttestationProviderInfo> GetAttestationProviders()
        {
            List<AttestationProviderInfo> providers = new List<AttestationProviderInfo>();

            if (ProviderCount > 1)
            {
                var (dnsNameBase, tenantNameBase) = ExtractBaseNames();
                for (int i = 0; i < ProviderCount; i++)
                {
                    providers.Add(CreateRangedProviderInfo(i, dnsNameBase, tenantNameBase));
                }
            }
            else
            {
                providers.Add(new AttestationProviderInfo()
                {
                    DnsName = DnsName,
                    TenantNameOverride = TenantNameOverride
                });
            }

            return providers;
        }

        private AttestationProviderInfo CreateRangedProviderInfo(int index, string dnsNameBase, string tenantNameBase)
        {
            var dnsName = string.IsNullOrEmpty(dnsNameBase) ? DnsName : $"{dnsNameBase}{index}";
            var tenantNameOverride = string.IsNullOrEmpty(tenantNameBase) ? TenantNameOverride : $"{tenantNameBase}{index}";

            return new AttestationProviderInfo()
            {
                DnsName = dnsName,
                TenantNameOverride = tenantNameOverride
            };
        }

        private (string, string) ExtractBaseNames()
        {
            var dnsNameBase = string.Empty;
            var tenantNameOverrideBase = string.Empty;

            if (!this.DnsName.Equals("localhost", StringComparison.InvariantCultureIgnoreCase))
            {
                var pre = Regex.Match(this.DnsName, ProviderDnsNameRegEx);
                dnsNameBase = pre.Groups[1].Value;
            }
            if (!string.IsNullOrEmpty(this.TenantNameOverride))
            {
                var tre = Regex.Match(this.TenantNameOverride, TenantNameRegEx);
                tenantNameOverrideBase = tre.Groups[1].Value;
            }

            return (dnsNameBase, tenantNameOverrideBase);
        }
    }
}
