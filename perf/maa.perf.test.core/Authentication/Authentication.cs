using maa.perf.test.core.Utils;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace maa.perf.test.core.Authentication
{
    public class Authentication
    {
        private const string authorityPrefix = "https://login.microsoftonline.com/";
        private const string clientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        private static readonly string[] scopes = new[] { "https://attest.azure.net/.default" };

        private const string AcceleratedTokenCacheFileName = "acceleratedcache.acache";
        private static Dictionary<string, string> _acceleratedCache;
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        static Authentication()
        {
            _acceleratedCache = SerializationHelper.ReadFromFile<Dictionary<string, string>>(AcceleratedTokenCacheFileName);
        }

        public static async Task<string> AcquireAccessTokenAsync(string tenant, bool forceRefresh, bool noUiPrompt)
        {
            string accessToken = null;

            // Only perform one authentication at a time!
            await _semaphore.WaitAsync();

            // Go go!
            try
            {
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

                    AuthenticationResult result = null;
                    try
                    {
                        var accounts = await publicApplication.GetAccountsAsync();
                        result = await publicApplication
                            .AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        if (!noUiPrompt)
                        {
                            result = await publicApplication
                                .AcquireTokenInteractive(scopes)
                                .WithClaims(ex.Claims)
                                .ExecuteAsync();
                        }
                    }

                    if (result != null)
                    {
                        accessToken = result.AccessToken;
                        _acceleratedCache[tenant] = accessToken;
                        SerializationHelper.WriteToFile(AcceleratedTokenCacheFileName, _acceleratedCache);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return accessToken;
        }
    }
}
