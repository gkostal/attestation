namespace maa.perf.test.core.Maa
{
    public class MaaConnectionInfo
    {
        public string DnsName { get; set; }
        public string TenantNameOverride { get; set; }
        public string ServicePort { get; set; }
        public bool UseHttp { get; set; }
        public bool ForceReconnects { get; set; }
    }
}
