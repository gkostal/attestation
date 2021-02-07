using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using maa.perf.test.core.Utils;
using Microsoft.Identity.Client;

namespace maa.perf.test.core.Authentication
{
    public class Authentication
    {
        private const string authorityPrefix = "https://login.microsoftonline.com/";
        private const string clientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        private static readonly string[] scopes = new []{ "https://attest.azure.net/.default" };

        private const string AcceleratedTokenCacheFileName = "acceleratedcache.bin";
        private static Dictionary<string, string> _acceleratedCache;
        private static object _lock = new object();
        private static bool _processingInFlight = false;

        static Authentication()
        {
            _acceleratedCache = SerializationHelper.ReadFromFile<Dictionary<string, string>>(AcceleratedTokenCacheFileName);
        }

        public static async Task<string> AcquireAccessTokenAsync(string tenant, bool forceRefresh)
        {
            string accessToken = null;
            bool okToProcess = false;

            try
            {
                while (!okToProcess)
                {
                    lock (_lock)
                    {
                        if (!_processingInFlight)
                        {
                            okToProcess = true;
                            _processingInFlight = true;
                        }
                    }

                    if (!_processingInFlight)
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                }

                if (!forceRefresh && _acceleratedCache.ContainsKey(tenant))
                {
                    accessToken = _acceleratedCache[tenant];
                }
                else
                {
                    var publicApplication = PublicClientApplicationBuilder.Create(clientId)
                        .WithAuthority($"{authorityPrefix}{tenant}")
                        .WithDefaultRedirectUri()
                        .Build();

                    AuthenticationResult result;
                    try
                    {
                        var accounts = await publicApplication.GetAccountsAsync();
                        Tracer.TraceInfo($"Authentication: Number of accounts: {accounts?.Count()}");
                        result = await publicApplication
                            .AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        result = await publicApplication
                            .AcquireTokenInteractive(scopes)
                            .WithClaims(ex.Claims)
                            .ExecuteAsync();
                    }

                    accessToken = result.AccessToken;
                    _acceleratedCache[tenant] = accessToken;
                    SerializationHelper.WriteToFile(AcceleratedTokenCacheFileName, _acceleratedCache);
                }
            }
            finally
            {
                if (okToProcess)
                {
                    lock (_lock)
                    {
                        _processingInFlight = false;
                        okToProcess = false;
                    }
                }
            }

            return accessToken;
        }
    }
}
