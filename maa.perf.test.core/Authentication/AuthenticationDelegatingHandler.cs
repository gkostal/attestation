using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using maa.perf.test.core.Utils;

namespace maa.perf.test.core.Authentication
{
    public class AuthenticationDelegatingHandler : DelegatingHandler
    {
        private readonly Dictionary<string, bool> UriAuthRequiredLookup;
        private readonly Dictionary<string, string> TenantLookup;
        private const string TenantLookupFileName = "tenantlookup.bin";
        private const string UriLookupFileName = "urilookup.bin";

        public AuthenticationDelegatingHandler()
            : base(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                MaxConnectionsPerServer = int.MaxValue
            })
        {
            TenantLookup = SerializationHelper.ReadFromFile<Dictionary<string, string>>(TenantLookupFileName);
            UriAuthRequiredLookup = SerializationHelper.ReadFromFile<Dictionary<string, bool>>(UriLookupFileName);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string aadTenant;
            string accessToken;
            string hostName = request.RequestUri.Host;
            string requestUri = request.RequestUri.AbsoluteUri;
            bool authenticationRequired = UriAuthRequiredLookup.ContainsKey(requestUri) && UriAuthRequiredLookup[requestUri];
            bool bearerTokenIncluded = false;

            // Get access token if auth is required and we already know the tenant for the attestation provider
            if (authenticationRequired && TenantLookup.ContainsKey(hostName))
            {
                aadTenant = TenantLookup[hostName];
                accessToken = await Authentication.AcquireAccessTokenAsync(aadTenant, false);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                bearerTokenIncluded = true;
            }

            // Call service
            var response = await base.SendAsync(request, cancellationToken);

            // Retry one time on unauthorized -- it's either because:
            //   * We didn't know the AAD tenant and didn't include a bearer token
            //   * The token expired and we need to refresh it from AAD
            // So, take note of current AAD tenant value, re-authenticate and retry
            if ((response.StatusCode == System.Net.HttpStatusCode.Unauthorized))
            {
                // Remember that authentication is required for this URI
                UriAuthRequiredLookup[requestUri] = true;
                SerializationHelper.WriteToFile(UriLookupFileName, UriAuthRequiredLookup);

                // Always record AAD tenant for hostname (in edge cases it can move)
                aadTenant = ParseAadTenant(response.Headers.GetValues("WWW-Authenticate").FirstOrDefault());
                TenantLookup[hostName] = aadTenant;
                SerializationHelper.WriteToFile(TenantLookupFileName, TenantLookup);

                // Authenticate with AAD and set bearer token
                accessToken = await Authentication.AcquireAccessTokenAsync(aadTenant, true);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                bearerTokenIncluded = true;

                // Retry one time
                response = await base.SendAsync(request, cancellationToken);
            }

            // If we succeeded without authentication, ensure that we remember that authentication
            // is not required for this URI
            if (response.IsSuccessStatusCode && !bearerTokenIncluded)
            {
                if (!UriAuthRequiredLookup.ContainsKey(requestUri) || UriAuthRequiredLookup[requestUri])
                {
                    // Remember that authentication is NOT required for this URI
                    UriAuthRequiredLookup[request.RequestUri.AbsoluteUri] = false;
                    SerializationHelper.WriteToFile(UriLookupFileName, UriAuthRequiredLookup);
                }
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
    }
}
