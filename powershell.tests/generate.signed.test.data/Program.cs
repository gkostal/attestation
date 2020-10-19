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
            List<X509Certificate2> myCerts = new List<X509Certificate2>();
            for (int i = 1; i <= 10; i++)
            {
                myCerts.Add(CertificateUtils.CreateCertificateAuthorityCertificate($"CN=MaaOriginalTestCert{i}"));
            }

            var firstCert = myCerts[0];
            var signingCert = new List<X509Certificate2>() {firstCert}.ToArray();

            Console.WriteLine($"Creating PEM certificates file: all.signing.certs.pem");
            File.WriteAllText($"{resultsDir}\\all.signing.certs.pem", CertificateUtils.GeneratePem(myCerts));

            Console.WriteLine($"Creating PEM certificate file: signing.cert.pem");
            File.WriteAllText($"{resultsDir}\\signing.cert.pem", CertificateUtils.GeneratePem(firstCert));

            // Create 4 additional signed certificates to add and remove
            for (int i=1; i<=20; i++)
            {
                X509Certificate2 cert = CertificateUtils.CreateCertificateAuthorityCertificate($"CN=MaaTestCert{i}");

                var exportedCert = cert.Export(X509ContentType.Cert);
                string jwkToAdd = $"{{\"kty\":\"RSA\", \"x5c\":[\"{System.Convert.ToBase64String(exportedCert)}\"]}}";
                string addCertBody = $"{{\"maa-policyCertificate\": {jwkToAdd}}}";
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
