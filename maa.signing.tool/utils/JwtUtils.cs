namespace maa.signing.tool.utils
{
    using System;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    public class JwtUtils
    {
        public static string GenerateSignedPolicyJsonWebToken(string policy, RSA signingKey, X509Certificate2 signingCert)
        {
            if (!policy.StartsWith('"'))
            {
                policy = "\"" + policy + "\"";
            }
            return GenerateSingleClaimJsonWebToken("AttestationPolicy", policy, signingKey, signingCert);
        }

        public static string GenerateSignedCertificateJsonWebToken(X509Certificate2 embeddedCertificate, RSA signingKey, X509Certificate2 signingCert)
        {
            var exportedCert = embeddedCertificate.Export(X509ContentType.Cert);
            string jwkToAdd = $"{{\"kty\":\"RSA\", \"x5c\":[\"{System.Convert.ToBase64String(exportedCert)}\"]}}";
            return GenerateSingleClaimJsonWebToken("maa-policyCertificate", jwkToAdd, signingKey, signingCert);
        }

        private static string GenerateSignedJsonWebToken(string jwtBody, RSA signingKey, X509Certificate2 signingCert)
        {
            // Encode header and body
            string encodedHeader = Base64Url.Encode(Encoding.UTF8.GetBytes(FormatJoseHeader(signingCert)));
            string encodedBody = Base64Url.Encode(Encoding.UTF8.GetBytes(jwtBody));

            // Sign
            var rawSignature = signingKey.SignData(Encoding.UTF8.GetBytes(encodedHeader + "." + encodedBody), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // Return JWT 
            return encodedHeader + "." + encodedBody + "." + Base64Url.Encode(rawSignature);
        }

        private static string GenerateSingleClaimJsonWebToken(string claimName, string claimValue, RSA signingKey, X509Certificate2 signingCert)
        {
            string jwtBody = "";
            jwtBody += "{";
            jwtBody += $"\"{claimName}\":{claimValue}";
            jwtBody += "}";

            return GenerateSignedJsonWebToken(jwtBody, signingKey, signingCert);
        }

        private static string FormatJoseHeader(X509Certificate2 signingCertificate)
        {
            string exportedCert = Convert.ToBase64String(signingCertificate.Export(X509ContentType.Cert));

            string joseHeader = "{ \"alg\":\"RS256\", \"x5c\": [";
            joseHeader += "\"" + exportedCert + "\"";
            joseHeader += "]}";
            return joseHeader;
        }
    }
}
