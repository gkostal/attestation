using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maa.perf.test.core.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ApiInfo
    {
        public ApiInfo()
        {
            ApiName = Api.None;
            UsePreviewApi = false;
            ServicePort = "";
            UseHttp = false;
            Weight = 0.0d;
            Percentage = 0.0d;
        }

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

}
