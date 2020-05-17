using System;
using System.Net.Http;
using Microsoft.IdentityModel.Tokens;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace validatequotes
{
    [Guid("46981BEA-6938-4D6F-8339-40C4CAC66E5B")]
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMetadataVerifier
    {
        bool VerifyQuoteExtensionInCertificate(
            [MarshalAs(UnmanagedType.LPStr)]string base64encodedCertificate);
        bool VerifyQuoteInExtension();
        bool VerifyCertificateKeyMatchesHash();

        uint SecurityVersion();
        void ProductId(ref int productIdSize, ref IntPtr productId);
        void UniqueId(ref int uniqueIdSize, ref IntPtr uniqueId);
        void SignerId(ref int signerIdSize, ref IntPtr signerId);
        void ReportData(ref int reportDataSize, ref IntPtr reportData);
        void PublicKeyHash(ref int publicKeyHashSize, ref IntPtr publicKeyHash);
    }

    public class VerifyMetadataCertificates
    {
        [DllImport("VerifyMetadataCertificates.dll")]
        public static extern int GetMetadataCertificateVerifier([Out]out IMetadataVerifier verifier);
    }


    class MaaQuoteValidator
    {
        static private KeyValuePair<int, string> FormatBufferLine(int startOffset, byte[] pb)
        {
            // scratch buffer which will hold the data being logged.
            StringBuilder sb = new StringBuilder();

            int bytesToWrite = pb.Length < 0x10 ? pb.Length : 0x10;

            sb.Append($"{startOffset,8:X8}: ");

            // Write the buffer data out.
            for (int i = 0; i < bytesToWrite; i += 1)
            {
                sb.Append($"{ pb[i + startOffset],2:X2} ");
            }

            // Now write the data in string format (similar to what the debugger does).
            // Start by padding partial lines to a fixed end.
            for (int i = bytesToWrite; i < 0x10; i += 1)
            {
                sb.Append("   ");
            }
            sb.Append("  * ");
            for (int i = 0; i < bytesToWrite; i += 1)
            {
                char charToWrite = (char)pb[i + startOffset];
                if (Char.IsWhiteSpace(charToWrite) || Char.IsLetterOrDigit(charToWrite) || Char.IsSymbol(charToWrite) || Char.IsPunctuation(charToWrite))
                {
                    sb.Append($"{charToWrite}");
                }
                else
                {
                    sb.Append(".");
                }
            }
            for (int i = bytesToWrite; i < 0x10; i += 1)
            {
                sb.Append(" ");
            }

            sb.Append(" *");

            sb.Append("\r\n");

            return new KeyValuePair<int, string>(bytesToWrite, sb.ToString());
        }

        static private string FormatBuffer(string prefix, byte[] buffer)
        {
            StringBuilder returnedString = new StringBuilder();
            int cb = buffer.Length;
            int currentOffset = 0;
            do
            {
                var stringToLog = FormatBufferLine(currentOffset, buffer);
                currentOffset += stringToLog.Key;
                cb -= stringToLog.Key;
                returnedString.Append(prefix);
                returnedString.Append(stringToLog.Value);
            } while (cb != 0);
            return returnedString.ToString();
        }

        static private byte[] ToByteArray(int size, IntPtr array)
        {
            byte[] byteArray = new byte[size];
            Marshal.Copy(array, byteArray, 0, size);
            Marshal.FreeCoTaskMem(array);
            return byteArray;
        }

        static public void ValidateMaaQuote(string x5c, bool includeDetails)
        {
            IMetadataVerifier certificateVerifier;
            VerifyMetadataCertificates.GetMetadataCertificateVerifier(out certificateVerifier);
            bool embeddedQuoteLocated = false;

            try
            {
                if (certificateVerifier.VerifyQuoteExtensionInCertificate(x5c))
                {
                    embeddedQuoteLocated = true;
                }
            }
            catch (Exception x)
            {
                Logger.WriteLine($"Exception thrown locating quote in certificate: {x.ToString()}");
            }

            Logger.WriteLine($"Embedded quote found in certificate: {embeddedQuoteLocated}");
            if (!embeddedQuoteLocated)
            {
                return;
            }

            var quoteVerifiedByIntel = certificateVerifier.VerifyQuoteInExtension();
            Logger.WriteLine($"Quote verified by Intel            : {quoteVerifiedByIntel}");
            if (!quoteVerifiedByIntel)
            {
                return;
            }

            if (includeDetails)
            {
                uint version = certificateVerifier.SecurityVersion();
                Logger.WriteLine($"    Security Version               : {version}");

                {
                    int productIdSize = 0;
                    IntPtr productIdRaw = IntPtr.Zero;
                    certificateVerifier.ProductId(ref productIdSize, ref productIdRaw);
                    byte[] productId = ToByteArray(productIdSize, productIdRaw);

                    Logger.WriteLine(37, 64, "    Product ID                     : ", BitConverter.ToString(productId).Replace("-", ""));
                }
                {
                    int signerIdSize = 0;
                    IntPtr signerIdRaw = IntPtr.Zero;
                    certificateVerifier.SignerId(ref signerIdSize, ref signerIdRaw);
                    byte[] signerId = ToByteArray(signerIdSize, signerIdRaw);

                    Logger.WriteLine(37, 64, "    Signer ID                      : ", BitConverter.ToString(signerId).Replace("-", ""));
                }

                {
                    int uniqueIdSize = 0;
                    IntPtr uniqueIdRaw = IntPtr.Zero;
                    certificateVerifier.UniqueId(ref uniqueIdSize, ref uniqueIdRaw);
                    byte[] uniqueId = ToByteArray(uniqueIdSize, uniqueIdRaw);

                    Logger.WriteLine(37, 64, "    Enclave ID                     : ", BitConverter.ToString(uniqueId).Replace("-", ""));
                }

                {
                    int reportDataSize = 0;
                    IntPtr reportDataRaw = IntPtr.Zero;
                    certificateVerifier.ReportData(ref reportDataSize, ref reportDataRaw);
                    byte[] reportData = ToByteArray(reportDataSize, reportDataRaw);

                    Logger.WriteLine(37, 64, "    Report data                    : ", BitConverter.ToString(reportData).Replace("-", ""));
                }

                {
                    int publicKeyHashSize = 0;
                    IntPtr publicKeyHashRaw = IntPtr.Zero;
                    certificateVerifier.PublicKeyHash(ref publicKeyHashSize, ref publicKeyHashRaw);
                    byte[] publicKeyHash = ToByteArray(publicKeyHashSize, publicKeyHashRaw);

                    Logger.WriteLine(37, 64, "    Public key hash                : ", BitConverter.ToString(publicKeyHash).Replace("-", ""));
                }
            }

            var signingCertKeyMatchesHash = certificateVerifier.VerifyCertificateKeyMatchesHash();
            Logger.WriteLine($"Signing cert key matches hash      : {signingCertKeyMatchesHash}");
        }

        static public void LocateAndValidateMaaQuote(string jwksForMaaTenant)
        {
            IMetadataVerifier certificateVerifier;

            var result = VerifyMetadataCertificates.GetMetadataCertificateVerifier(out certificateVerifier);
            Console.WriteLine("Retrieve Metadata Signing Certificates from MAA");

            bool foundExtension = false;

            {
                string urljwksBytes = jwksForMaaTenant;

                Microsoft.IdentityModel.Tokens.JsonWebKeySet keySet = new Microsoft.IdentityModel.Tokens.JsonWebKeySet(urljwksBytes);

                foreach (var key in keySet.Keys)
                {
                    if (key.Kty == "RSA")
                    {
                        if (key.X5c.Count != 0)
                        {
                            var base64Cert = key.X5c[0];
                            try
                            {
                                if (certificateVerifier.VerifyQuoteExtensionInCertificate(base64Cert))
                                {
                                    foundExtension = true;
                                    break;  
                                }
                            }
                            catch (Exception)
                            {
                                // Ignore!
                            }
                        }
                    }
                }

                if (!foundExtension)
                {
                    Console.WriteLine("Could not find SGX quote extension in any of the provided certificates.");
                    return;
                }

                Console.WriteLine("Found a certificate which contains an embedded SGX Quote ");

                if (!certificateVerifier.VerifyQuoteInExtension())
                {
                    Console.WriteLine("Could not verify SGX quote extension the certificate.");
                    return;
                }

                Console.WriteLine("SGX Quote has been successfully verified.");


                Console.WriteLine("Parsed SGX Report: ");

                uint version = certificateVerifier.SecurityVersion();
                Console.WriteLine($"  Security Version: {version}");

                {
                    int productIdSize = 0;
                    IntPtr productIdRaw = IntPtr.Zero;
                    certificateVerifier.ProductId(ref productIdSize, ref productIdRaw);
                    byte[] productId = ToByteArray(productIdSize, productIdRaw);

                    Console.WriteLine("{0}", FormatBuffer("        Product ID: ", productId));
                }
                {
                    int signerIdSize = 0;
                    IntPtr signerIdRaw = IntPtr.Zero;
                    certificateVerifier.SignerId(ref signerIdSize, ref signerIdRaw);
                    byte[] signerId = ToByteArray(signerIdSize, signerIdRaw);

                    Console.WriteLine("{0}", FormatBuffer("         Signer ID: ", signerId));
                }

                {
                    int uniqueIdSize = 0;
                    IntPtr uniqueIdRaw = IntPtr.Zero;
                    certificateVerifier.UniqueId(ref uniqueIdSize, ref uniqueIdRaw);
                    byte[] uniqueId = ToByteArray(uniqueIdSize, uniqueIdRaw);
                    Console.WriteLine("{0}", FormatBuffer("        Enclave ID: ", uniqueId));
                }

                {
                    int reportDataSize = 0;
                    IntPtr reportDataRaw = IntPtr.Zero;
                    certificateVerifier.ReportData(ref reportDataSize, ref reportDataRaw);
                    byte[] reportData = ToByteArray(reportDataSize, reportDataRaw);
                    Console.WriteLine("{0}", FormatBuffer("       report data: ", reportData));

                }

                {
                    int publicKeyHashSize = 0;
                    IntPtr publicKeyHashRaw = IntPtr.Zero;
                    certificateVerifier.PublicKeyHash(ref publicKeyHashSize, ref publicKeyHashRaw);
                    byte[] publicKeyHash = ToByteArray(publicKeyHashSize, publicKeyHashRaw);
                    Console.WriteLine("{0}", FormatBuffer("   public key hash: ", publicKeyHash));
                }

                if (!certificateVerifier.VerifyCertificateKeyMatchesHash())
                {
                    Console.WriteLine("Could not verify that key hash matches the quote hash.");
                    return;
                }
                Console.WriteLine("Verified that certificate key matches the hash.");

            }
        }
    }
}
