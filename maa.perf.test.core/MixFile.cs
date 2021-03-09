using maa.perf.test.core.Utils;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace maa.perf.test.core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ApiInfo
    {
        [JsonProperty]
        public Api ApiName { get; set; }
        [JsonProperty]
        public double Weight { get; set; }

        public double Percentage { get; set; }
    }

    public class MixFile
    {
        public List<ApiInfo> ApiMix { get; set; }

        public static MixFile GetMixFile(string mixFileName)
        {
            var mixFileContents = SerializationHelper.ReadFromFile<MixFile>(mixFileName);
            var totalWeight = 0.0d;

            foreach (var a in mixFileContents.ApiMix)
            {
                totalWeight += a.Weight;
            }

            foreach (var a in mixFileContents.ApiMix)
            {
                a.Percentage = a.Weight / totalWeight;
            }

            return mixFileContents;
        }
    }
}
