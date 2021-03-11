using maa.perf.test.core.Utils;
using System.Collections.Generic;
using System.Linq;

namespace maa.perf.test.core.Model
{
    public class MixInfo
    {
        public MixInfo()
        {
            ApiMix = new List<WeightedApiInfo>();
            ProviderMix = new List<WeightedAttestationProvidersInfo>();
        }

        public List<WeightedApiInfo> ApiMix { get; set; }

        public List<WeightedAttestationProvidersInfo> ProviderMix { get; set; }

        public static MixInfo ReadMixInfo(string mixFileName)
        {
            var mixFileContents = default(MixInfo);

            if (!string.IsNullOrEmpty(mixFileName))
            {
                mixFileContents = SerializationHelper.ReadFromFile<MixInfo>(mixFileName);
                var totalApiWeight = mixFileContents.ApiMix?.Sum(a => a.Weight);
                var totalProviderWeight = mixFileContents.ProviderMix?.Sum(p => p.Weight);

                mixFileContents.ApiMix?.ForEach(a => a.Percentage = a.Weight / totalApiWeight.Value);
                mixFileContents.ProviderMix?.ForEach(p => p.Percentage = p.Weight / totalProviderWeight.Value);
            }

            return mixFileContents;
        }
    }
}
