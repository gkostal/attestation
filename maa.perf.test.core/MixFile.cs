using maa.perf.test.core.Utils;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace maa.perf.test.core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ApiInfo
    {
        [JsonProperty]
        public Api ApiName { get; set; }
        [JsonProperty]
        public bool UsePreviewApi { get; set; }
        [JsonProperty]
        public string ServicePort { get; set; }
        [JsonProperty]
        public bool UseHttp { get; set; }
        [JsonProperty]
        public double Weight { get; set; }

        public double Percentage { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AttestationProviderInfo
    {
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

    public class MixFile
    {
        public List<ApiInfo> ApiMix { get; set; }

        public List<AttestationProviderInfo> ProviderMix { get; set; }

        public static MixFile GetMixFile(string mixFileName)
        {
            var mixFileContents = default(MixFile);

            if (!string.IsNullOrEmpty(mixFileName))
            {
                mixFileContents = SerializationHelper.ReadFromFile<MixFile>(mixFileName);
                var totalApiWeight = mixFileContents.ApiMix?.Sum(a => a.Weight);
                var totalProviderWeight = mixFileContents.ProviderMix?.Sum(p => p.Weight);

                mixFileContents.ApiMix?.ForEach(a => a.Percentage = a.Weight / totalApiWeight.Value);
                mixFileContents.ProviderMix?.ForEach(p => p.Percentage = p.Weight / totalProviderWeight.Value);
            }

            return mixFileContents;
        }
    }
}
