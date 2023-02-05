namespace maa.perf.test.core.Model
{
    public enum ApiVersion
    {
        Unknown,
        Preview,
        GA,
    };

    public static class ApiVersionResolver
    {
        public static string Resolve(this ApiVersion apiVersion)
        {
            switch (apiVersion)
            {
                case ApiVersion.Preview:
                    return "2018-09-01-preview";
                case ApiVersion.GA:
                    return "2020-10-01";
                case ApiVersion.Unknown:
                default:
                    throw new System.Exception("Unknown api-version encountered!");
            }
        }
    }
}
