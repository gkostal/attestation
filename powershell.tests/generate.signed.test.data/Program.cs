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
            var sourceDir = @"..\..\..\unsigned.data.for.test";
            var resultsDir = @"..\..\..\signed.data.for.test";

            Directory.CreateDirectory(resultsDir);

            // Create the original trusted policy signing certificate
            X509Certificate2 cert1 = CertificateUtils.CreateCertificateAuthorityCertificate("CN=MaaTestCert1");
            var signingCert = new List<X509Certificate2>() {cert1}.ToArray();

            Console.WriteLine($"Creating PEM certificate file: cert1.pem");
            File.WriteAllText($"{resultsDir}\\cert1.pem", CertificateUtils.GeneratePem(cert1));

            // Create 4 additional signed certificates to add and remove
            for (int i=2; i<=5; i++)
            {
                X509Certificate2 cert = CertificateUtils.CreateCertificateAuthorityCertificate($"CN=MaaTestCert{i}");

                var exportedCert = cert.Export(X509ContentType.Cert);
                string jwkToAdd = $"{{\"kty\":\"RSA\", \"x5c\":[\"{System.Convert.ToBase64String(exportedCert)}\"]}}";
                string addCertBody = $"{{\"aas-policyCertificate\": {jwkToAdd}}}";
                string certAddJwt = JwtUtils.GenerateSignedJsonWebToken(addCertBody, signingCert);
                Console.WriteLine($"Creating signed certificate file: cert{i}.signed.txt");
                File.WriteAllText($"{resultsDir}\\cert{i}.signed.txt", certAddJwt);
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
        }
    }
}
