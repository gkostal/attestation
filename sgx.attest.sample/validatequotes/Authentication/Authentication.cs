using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;

namespace validatequotes
{
    public class Authentication
    {
        private const string resource = "https://attest.azure.net";
        private const string clientId = "1950a258-227b-4e31-a9cf-717495945fc2";

        public static async Task<string> AcquireAccessTokenAsync(string tenant)
        {
            var ctx = new AuthenticationContext($"https://login.microsoftonline.com/{tenant}");
            DeviceCodeResult codeResult = await ctx.AcquireDeviceCodeAsync(resource, clientId);
            Logger.WriteLine("Please sign into your AAD account.");
            Logger.WriteLine($"{codeResult.Message}");
            Logger.WriteLine("");
            return (await ctx.AcquireTokenByDeviceCodeAsync(codeResult)).AccessToken;
        }
    }
}
