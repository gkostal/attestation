using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace validatequotes.Helpers
{
    class JwtValidationHelper
    {
        public static TokenValidationResult ValidateMaaJwt(string attestDnsName, string serviceJwt)
        {
            var tenantName = attestDnsName.Split('.')[0];
            var attestUri = $"https://{attestDnsName}";

            var jwksTrustedSigningKeys = RetrieveTrustedSigningKeys(serviceJwt, attestDnsName, tenantName);

            var validatedToken = ValidateSignedToken(serviceJwt, jwksTrustedSigningKeys);
            ValidateJwtIssuerIsTenant(validatedToken, attestUri);
            ValidateSigningCertIssuerMatchesJwtIssuer(validatedToken);

            return validatedToken;
        }

        #region Internal implementation details

        private static void ValidateSigningCertIssuerMatchesJwtIssuer(TokenValidationResult validatedToken)
        {
            var jwtTokenIssuerClaim = validatedToken.ClaimsIdentity.Claims.First(c => c.Type == "iss");

            // Ensure that the JWT signing certificate is issued by the same issuer as the JWT itself
            var validatedKey = validatedToken.SecurityToken.SigningKey;
            if (!(validatedKey is X509SecurityKey))
            {
                throw new ArgumentException("JWT is not valid (signing key is not an X509 security key)");
            }
            var signingCertificate = (validatedKey as X509SecurityKey).Certificate;
            if (!string.Equals(signingCertificate.Issuer, "CN=" + jwtTokenIssuerClaim.Value, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("JWT is not valid (signing certificate issuer does not match JWT issuer)");
            }
            Logger.WriteLine($"JWT signing cert issuer validation: True");
        }

        private static void ValidateJwtIssuerIsTenant(TokenValidationResult validatedToken, string tenantAttestUri)
        {
            // Verify that the JWT issuer is indeed the tenantAttestUri (tenant specific URI)
            var jwtTokenIssuerClaim = validatedToken.ClaimsIdentity.Claims.First(c => c.Type == "iss");
            if (!string.Equals(tenantAttestUri, jwtTokenIssuerClaim.Value, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("JWT is not valid (iss claim does not match attest URI)");
            }
            Logger.WriteLine($"JWT issuer claim validation:        True");
        }

        private static TokenValidationResult ValidateSignedToken(string serviceJwt, JsonWebKeySet jwksTrustedSigningKeys)
        {
            // Now validate the JWT using the signing keys we just discovered
            TokenValidationParameters tokenValidationParams = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                IssuerSigningKeys = jwksTrustedSigningKeys.GetSigningKeys()
            };
            var jwtHandler = new JsonWebTokenHandler();
            var validatedToken = jwtHandler.ValidateToken(serviceJwt, tokenValidationParams);
            if (!validatedToken.IsValid)
            {
                throw new ArgumentException("JWT is not valid (signature verification failed)");
            }

            Logger.WriteLine($"JWT signature validation:           True");
            return validatedToken;
        }

        private static JsonWebKeySet RetrieveTrustedSigningKeys(string serviceJwt, string attestDnsName, string tenantName)
        {
            var expectedCertificateDiscoveryEndpoint = $"https://{attestDnsName}/certs";

            // Parse attestation service trusted signing key discovery endpoint from JWT header jku field
            var jwt = new JsonWebToken(serviceJwt);
            var jsonHeaderBytes = Base64Url.DecodeBytes(jwt.EncodedHeader);
            var jsonHeaderString = Encoding.UTF8.GetString(jsonHeaderBytes);
            var jsonHeader = JObject.Parse(jsonHeaderString);
            var jkuUri = jsonHeader.SelectToken("jku");
            Uri certificateDiscoveryEndpoint = new Uri(jkuUri.ToString());

            // Validate that "jku" points to the expected certificate discovery endpoint
            if (!expectedCertificateDiscoveryEndpoint.Equals(certificateDiscoveryEndpoint.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException($"JWT JKU header not valid.  Value is '{certificateDiscoveryEndpoint.ToString()}'.  Expected value is '{expectedCertificateDiscoveryEndpoint}'");
            }

            // Retrieve trusted signing keys from the attestation service
            var webClient = new WebClient();
            webClient.Headers.Add("tenantName", tenantName.Length > 24 ? tenantName.Remove(24) : tenantName);
            var jwksValue = webClient.DownloadString(certificateDiscoveryEndpoint);

            return new JsonWebKeySet(jwksValue);
        }

        #endregion
    }
}
