namespace maa.perf.test.core.Model
{
    using System;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    public class AttestationProviderInfo
    {
        const string TenantNameRegEx = @"(\D*)(\d*)";
        const string ProviderDnsNameRegEx = @"((\D+\d*\D+)\d+[.]?)(.*)";

        public AttestationProviderInfo()
        {
            DnsName = "";
            TenantNameOverride = "";
        }

        [JsonProperty]
        public string DnsName { get; set; }
        [JsonProperty]
        public string IpAddress { get; set; }
        [JsonProperty]
        public string TenantNameOverride { get; set; }

        public (string, string, string) ExtractBaseNames()
        {
            var dnsNameBase = string.Empty;
            var dnsSubDomain = string.Empty;
            var tenantNameOverrideBase = string.Empty;

            if (!this.DnsName.Equals("localhost", StringComparison.InvariantCultureIgnoreCase))
            {
                var pre = Regex.Match(this.DnsName, ProviderDnsNameRegEx);
                dnsNameBase = pre.Groups[2].Value;
                dnsSubDomain = $".{pre.Groups[3].Value}";
            }
            if (!string.IsNullOrEmpty(this.TenantNameOverride))
            {
                var tre = Regex.Match(this.TenantNameOverride, TenantNameRegEx);
                tenantNameOverrideBase = tre.Groups[1].Value;
            }

            return (dnsNameBase, dnsSubDomain, tenantNameOverrideBase);
        }
    }
}