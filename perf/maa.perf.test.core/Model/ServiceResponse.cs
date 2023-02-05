namespace maa.perf.test.core.Model
{
    public class ServiceResponse
    {
        public int StatusCode { get; }
        public string Body { get; }
        public PerformanceInformation PerfInfo { get; } = new PerformanceInformation();
        public string ServiceVersion { get; }
        public bool Success { get; }

        public ServiceResponse()
        {
            StatusCode = 500;
            Body = string.Empty;
            ServiceVersion = string.Empty;
            Success = false;
        }

        public ServiceResponse(int statusCode, string body, PerformanceInformation perfInfo, string serviceVersion)
        {
            StatusCode = statusCode;
            Body = body;
            PerfInfo = perfInfo;
            ServiceVersion = serviceVersion;
            Success = statusCode < 500;
        }
    }
}
