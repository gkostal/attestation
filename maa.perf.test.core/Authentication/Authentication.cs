using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using maa.perf.test.core.Utils;

namespace maa.perf.test.core.Authentication
{
    public class Authentication
    {
        private const string resource = "https://attest.azure.net";
        private const string clientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        private const string TokenCacheFileName = "tokencache.bin";
        private const string AcceleratedTokenCacheFileName = "acceleratedcache.bin";
        private static Dictionary<string, string> _acceleratedCache;
        private static TokenCache _tokenCache;
        private static object _lock = new object();
        private static bool _processingInFlight = false;

        private class ByteArrayWrapper
        {
            public byte[] theBytes;
        }

        static Authentication()
        {
            var baw = SerializationHelper.ReadFromFile<ByteArrayWrapper>(TokenCacheFileName);
            _tokenCache = new TokenCache();
            _tokenCache.DeserializeAdalV3(baw.theBytes);

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
                    var ctx = new AuthenticationContext($"https://login.windows.net/{tenant}", _tokenCache);

                    try
                    {
                        accessToken = (await ctx.AcquireTokenSilentAsync(resource, clientId)).AccessToken;
                        _acceleratedCache[tenant] = accessToken;
                    }
                    catch (AdalException x)
                    {
                        Tracer.TraceRaw($"");
                        Tracer.TraceRaw($"Silent token acquisition failed.");
                        Tracer.TraceRaw($"ADAL Exception: {x.Message}");
                        Tracer.TraceRaw($"Retrieving token via device code authentication now.");
                        Tracer.TraceRaw($"");

                        DeviceCodeResult codeResult = await ctx.AcquireDeviceCodeAsync(resource, clientId);
                        Tracer.TraceRaw("Please sign into your AAD account.");
                        Tracer.TraceRaw($"{codeResult.Message}");
                        Tracer.TraceRaw("");
                        Tracer.TraceRaw($"");
                        accessToken = (await ctx.AcquireTokenByDeviceCodeAsync(codeResult)).AccessToken;
                        SerializationHelper.WriteToFile(TokenCacheFileName, new ByteArrayWrapper { theBytes = _tokenCache.SerializeAdalV3() });
                    }
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
