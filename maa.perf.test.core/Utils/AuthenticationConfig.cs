namespace maa.perf.test.core.Utils
{
    internal interface IAuthenticationConfig
    {
        string Tenant { get; }
        string AadAuthority { get; }
        string AuthenticatedResourceId { get; }
        string NativeClientId { get; }
        string NativeClientRedirectUri { get; }
    }

    internal class ProdAsPsh : IAuthenticationConfig
    {
        public static IAuthenticationConfig AuthenticationConfig => new ProdAsPsh();

        public string Tenant => "microsoft.com";
        public string AadAuthority => "https://login.windows.net/";
        public string AuthenticatedResourceId => "https://attest.azure.net";
        public string NativeClientId => "1950a258-227b-4e31-a9cf-717495945fc2";
        public string NativeClientRedirectUri => "urn:ietf:wg:oauth:2.0:oob";
    }

    internal static class AuthExtensions
    {
        public static string GetAadAuthority(this IAuthenticationConfig authenticationConfig)
        {
            return authenticationConfig.AadAuthority + authenticationConfig.Tenant;
        }
    }
}
