using System.Linq;

namespace AasPolicyCertificates
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
            var numOriginalTrustedCerts = 10;
            var numPossibleNewTrustedCerts = 20;
            var sourceDir = @"..\..\..\unsigned.data.for.test";
            var resultsDir = @"..\..\..\signed.data.for.test";
            var rawSourceDir = @"..\..\..\raw.unsigned.data.for.test";
            var rawResultsDir = @"..\..\..\raw.signed.data.for.test";

            Directory.CreateDirectory(resultsDir);

            // Generate sample PEM with a certificate chain
            var parentCert = CertificateUtils.CreateCertificateAuthorityCertificate($"CN=MyCaCertificate");
            var intermediateCert = CertificateUtils.IssueCertificate($"CN=MyLeafCertificate", parentCert, false);
            var leafCert = CertificateUtils.IssueCertificate($"CN=MyLeafCertificate", intermediateCert, true);
            var myCertChain = new List<X509Certificate2>() { leafCert, intermediateCert, parentCert };
            Console.WriteLine($"Creating PEM file with a cert chain: cert.chain.pem");
            File.WriteAllText($"{resultsDir}\\cert.chain.pem", CertificateUtils.GeneratePem(myCertChain));

            // Generate 10 sample PEM's with a self signed certificate
            // And also one PEM file with all 10 self signed certificates
            List<X509Certificate2> mySelfSignedCerts = new List<X509Certificate2>();
            Enumerable.Range(1, numOriginalTrustedCerts).ForEach(i => mySelfSignedCerts.Add(CertificateUtils.CreateCertificateAuthorityCertificate($"CN=MaaOriginalTestCert{i}")));

            var firstCert = mySelfSignedCerts[0];
            var signingCert = new List<X509Certificate2>() { firstCert }.ToArray();

            Console.WriteLine($"Creating PEM certificates file: ten.self.signed.signing.certs.pem");
            File.WriteAllText($"{resultsDir}\\ten.self.signed.signing.certs.pem", CertificateUtils.GeneratePem(mySelfSignedCerts));

            Console.WriteLine($"Creating PEM certificate file: signing.cert.pem");
            File.WriteAllText($"{resultsDir}\\signing.cert.pem", CertificateUtils.GeneratePem(firstCert));

            // Create 20 additional signed certificates to add and remove
            Enumerable.Range(1, numPossibleNewTrustedCerts).ForEach(i =>
            {
                X509Certificate2 cert = CertificateUtils.CreateCertificateAuthorityCertificate($"CN=MaaTestCert{i}");

                var exportedCert = cert.Export(X509ContentType.Cert);
                string jwkToAdd = $"{{\"kty\":\"RSA\", \"x5c\":[\"{System.Convert.ToBase64String(exportedCert)}\"]}}";
                string addCertBody = $"{{\"maa-policyCertificate\": {jwkToAdd}}}";
                string certAddJwt = JwtUtils.GenerateSignedJsonWebToken(addCertBody, signingCert);
                Console.WriteLine($"Creating signed certificate file: cert{i}.signed.txt");
                File.WriteAllText($"{resultsDir}\\cert{i}.signed.txt", certAddJwt);
            });

            // Create a signed certificate chain to add and remove
            {
                var exportedParentCert = parentCert.Export(X509ContentType.Cert);
                var exportedIntermediateCert = intermediateCert.Export(X509ContentType.Cert);
                var exportedLeafCert = leafCert.Export(X509ContentType.Cert);

                string jwkToAdd = $"{{\"kty\":\"RSA\", \"x5c\":[\"{System.Convert.ToBase64String(exportedLeafCert)}\", \"{System.Convert.ToBase64String(exportedIntermediateCert)}\", \"{System.Convert.ToBase64String(exportedParentCert)}\"]}}";
                string addCertBody = $"{{\"maa-policyCertificate\": {jwkToAdd}}}";
                string certAddJwt = JwtUtils.GenerateSignedJsonWebToken(addCertBody, signingCert);

                Console.WriteLine($"Creating signed certificate file with cert chain: cert.chain.signed.txt");
                File.WriteAllText($"{resultsDir}\\cert.chain.signed.txt", certAddJwt);
            }

            // Create a signed version of all unsigned policy files
            foreach (var file in Directory.EnumerateFiles(sourceDir))
            {
                var fileInfo = new FileInfo(file);
                var encodedBody = File.ReadAllText(file).Split('.')[1];
                var decodedBody = Encoding.UTF8.GetString(Base64Url.Decode(encodedBody));
                var signedPolicyJwt = JwtUtils.GenerateSignedJsonWebToken(decodedBody, signingCert);
                Console.WriteLine($"Creating signed policy file: {fileInfo.Name}.signed{fileInfo.Extension}");
                File.WriteAllText($"{resultsDir}\\{fileInfo.Name}.signed{fileInfo.Extension}", signedPolicyJwt);
            }

            // Create a signed version of all raw unsigned policy files
            foreach (var file in Directory.EnumerateFiles(rawSourceDir))
            {
                var fileInfo = new FileInfo(file);
                var policy = File.ReadAllText(file);
                policy = policy.Replace("\n", @"\n");
                policy = policy.Replace("\r", @"\r");
                var signedPolicyJwt = JwtUtils.GenerateSignedPolicyJsonWebToken(policy, signingCert);
                Console.WriteLine($"Creating signed policy file: {fileInfo.Name}.signed{fileInfo.Extension}");
                File.WriteAllText($"{rawResultsDir}\\{fileInfo.Name}.signed{fileInfo.Extension}", signedPolicyJwt);
            }
        }
    }

    public static class MoreLinq
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
            {
                action(element);
            }
        }
    }
}
