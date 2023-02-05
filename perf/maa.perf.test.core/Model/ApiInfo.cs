namespace maa.perf.test.core.Model
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

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
            Headers = string.Empty;
            PostUrl = string.Empty;
            PostFile = string.Empty;
            PutUrl = string.Empty;
            PutFile = string.Empty;
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
        [JsonProperty]
        public string Headers { get; set; }
        [JsonProperty]
        public string PostUrl { get; set; }
        [JsonProperty]
        public string PostFile { get; set; }
        [JsonProperty]
        public string PutUrl { get; set; }
        [JsonProperty]
        public string PutFile { get; set; }

        public Dictionary<string, string> HeadersAsDictionary()
        {
            var allHeaders = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(this.Headers))
            {
                var theHeaders = this.Headers.Split(';');
                foreach (var singleHeader in theHeaders)
                {
                    var theHeaderComponents = singleHeader.Split('=');
                    allHeaders[theHeaderComponents[0]] = theHeaderComponents[1];
                }
            }

            return allHeaders;
        }
    }
}
