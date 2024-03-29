﻿namespace maa.perf.test.core.Model
{
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    public class WeightedApiInfo : ApiInfo, IWeightedObject
    {
        public WeightedApiInfo()
        {
            Weight = 0.0d;
            Percentage = 0.0d;
        }

        [JsonProperty]
        public double Weight { get; set; }

        public double Percentage { get; set; }
    }
}