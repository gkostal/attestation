namespace maa.perf.test.core.Maa
{
    using maa.perf.test.core.Model;
    using System.Collections.Generic;

    public class MaaConnectionInfo
    {
        public string DnsName { get; set; }
        public string IpAddress { get; set; }
        public string TenantNameOverride { get; set; }
        public string ServicePort { get; set; }
        public bool UseHttp { get; set; }
        public bool ForceReconnects { get; set; }

        public Dictionary<string, string> AdditionalHeaders { get; set; } = new Dictionary<string, string>();

        public static implicit operator AttestationProviderInfo(MaaConnectionInfo ci)
        {
            return new AttestationProviderInfo()
            {
                DnsName = ci.DnsName,
                TenantNameOverride = ci.TenantNameOverride
            };
        }

        public string GetUrlSchemeName()
        {
            return IpAddress ?? DnsName;
        }
        
        public string GetTenantName()
        {
            AttestationProviderInfo x = this;
            var (dnsName, dnsSubdomainName, tenantNameOverride) = x.ExtractBaseNames();
            return string.IsNullOrEmpty(dnsName) ? tenantNameOverride : dnsName;
        }
    }
}
