namespace AasPolicyCertificates
{
    using System;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    public class JwtUtils
    {
        private static readonly HashAlgorithm hashAlgorithm = new SHA256CryptoServiceProvider();

        internal static string GenerateSignedJsonWebToken(string jwtBody, X509Certificate2[] signingCerts)
        {
            // Encode header and body
            string encodedHeader = Base64Url.Encode(Encoding.UTF8.GetBytes(FormatJoseHeader(signingCerts)));
            string encodedBody = Base64Url.Encode(Encoding.UTF8.GetBytes(jwtBody));

            // Sign
            var rawSignature = GetRsaSigner(signingCerts).SignData(Encoding.UTF8.GetBytes(encodedHeader + "." + encodedBody), hashAlgorithm);

            // Return JWT 
            return encodedHeader + "." + encodedBody + "." + Base64Url.Encode(rawSignature);
        }

        internal static string GenerateSignedPolicyJsonWebToken(string policyDocument, X509Certificate2[] signingCerts)
        {
            string jwtBody = "";
            jwtBody += "{";
            jwtBody += $"\"AttestationPolicy\":\"{policyDocument}\"";
            jwtBody += "}";

            return GenerateSignedJsonWebToken(jwtBody, signingCerts);
        }

        private static RSACryptoServiceProvider GetRsaSigner(X509Certificate2[] signingCerts)
        {
            // Export the key info from the certificate signer into a new crypto service provider
            // to ensure that the RSA AES crypto provider is provided.
            var certSigner = (RSACryptoServiceProvider) signingCerts[0].PrivateKey;
            RSACryptoServiceProvider signer = new RSACryptoServiceProvider();
            signer.ImportParameters(certSigner.ExportParameters(true));
            return signer;
        }

        private static string FormatJoseHeader(X509Certificate2[] signingCertificates)
        {
            string joseHeader = "{ \"alg\":\"RS256\", \"x5c\": [";
            string prependDelimiter = "";
            foreach (var cert in signingCertificates)
            {
                string exportedCert = Convert.ToBase64String(cert.Export(X509ContentType.Cert));
                joseHeader += prependDelimiter + "\"" + exportedCert + "\"";
                prependDelimiter = ", ";
            }

            joseHeader += "]}";
            return joseHeader;
        }
    }
}
