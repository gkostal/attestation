using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using maa.signing.tool.utils;

namespace maa.signing.tool
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Load signing key and certificate
            var signingKey = RSA.Create();
            //signingKey.ImportFromPem(signingKeyPemText.ToCharArray());
            var signingKeyPemText = File.ReadAllText(@".\certs\sample.signing.cert.encrypted.key");
            signingKey.ImportFromEncryptedPem(signingKeyPemText, "12345");
            var signingCert = new X509Certificate2(@".\certs\sample.signing.cert.encrypted.crt");

            // Create signed policy for upload
            var denyAllPolicy = File.ReadAllText(@".\policy\deny.all.txt").Replace("\n", @"\n").Replace("\r", @"\r");
            var policyJwt = JwtUtils.GenerateSignedPolicyJsonWebToken(denyAllPolicy, signingKey, signingCert);
            File.WriteAllText(@".\signed.policy.jwt.txt", policyJwt);
            Console.WriteLine($"\nPolicy JWT: {File.ReadAllText(@".\signed.policy.jwt.txt")}");

            // Create signed certificate for upload
            var cert = new X509Certificate2(@".\certs\sample.signing.cert.crt");
            var certJwt = JwtUtils.GenerateSignedCertificateJsonWebToken(cert, signingKey, signingCert);
            File.WriteAllText(@".\signed.cert.jwt.txt", certJwt);
            Console.WriteLine($"\nCert JWT: {File.ReadAllText(@".\signed.cert.jwt.txt")}");
        }
    }
}