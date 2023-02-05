using Newtonsoft.Json;

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
            Url = string.Empty;
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
        public string Url { get; set; }
    }
}
