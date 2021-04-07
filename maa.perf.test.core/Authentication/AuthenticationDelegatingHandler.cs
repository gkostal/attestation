using maa.perf.test.core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace maa.perf.test.core.Authentication
{
    public class AuthenticationDelegatingHandler : DelegatingHandler
    {
        private readonly Dictionary<string, string> TenantLookup;
        private const string TenantLookupFileName = "tenantlookup.bin";
        private const string ProviderDnsNameRegEx = @"((\D+\d*\D+)\d+[.]?)(.*)";

        public AuthenticationDelegatingHandler()
            : base(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                MaxConnectionsPerServer = int.MaxValue
            })
        {
            TenantLookup = SerializationHelper.ReadFromFile<Dictionary<string, string>>(TenantLookupFileName);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string aadTenant;
            string accessToken;
            string transformedHostName = TransformHostName(request.RequestUri.Host);

            // If we know the tenant for the attestation provider and already have an access token, 
            // just include it!
            if (TenantLookup.ContainsKey(transformedHostName))
            {
                aadTenant = TenantLookup[transformedHostName];
                accessToken = await Authentication.AcquireAccessTokenAsync(aadTenant, false, true);
                if (accessToken != null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
            }

            // Call service
            var response = await base.SendAsync(request, cancellationToken);

            // Retry one time on unauthorized -- it's either because:
            //   * We didn't know the AAD tenant and didn't include a bearer token
            //   * The token expired and we need to refresh it from AAD
            // So, take note of current AAD tenant value, re-authenticate and retry
            if ((response.StatusCode == System.Net.HttpStatusCode.Unauthorized))
            {
                // Always record AAD tenant for hostname (in edge cases it can move)
                aadTenant = ParseAadTenant(response.Headers.GetValues("WWW-Authenticate").FirstOrDefault());
                TenantLookup[transformedHostName] = aadTenant;
                SerializationHelper.WriteToFile(TenantLookupFileName, TenantLookup);

                // Authenticate with AAD and set bearer token
                accessToken = await Authentication.AcquireAccessTokenAsync(aadTenant, true, false);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Retry one time
                response = await base.SendAsync(request, cancellationToken);
            }

            return response;
        }

        private string ParseAadTenant(string headerValue)
        {
            // Bearer authorization_uri="https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47", resource="https://attest.azure.net"
            const string startString = "login.windows.net/";
            const string endString = "\"";

            var startIndex = headerValue.IndexOf(startString) + startString.Length;
            var endIndex = headerValue.IndexOf(endString, startIndex);

            return headerValue.Substring(startIndex, endIndex - startIndex);
        }

        private string TransformHostName(string hostName)
        {
            // aaa000.wus.attest.azure.net is considered to have the same AAD tenant as aaa111.wus.attest.azure.net
            if (!hostName.Equals("localhost", StringComparison.InvariantCultureIgnoreCase))
            {
                var pre = Regex.Match(hostName, ProviderDnsNameRegEx);
                var dnsNameBase = pre.Groups[2].Value;
                var dnsSubDomain = $".{pre.Groups[3].Value}";
                return $"{dnsNameBase}{dnsSubDomain}";
            }

            return hostName;
        }
    }
}
