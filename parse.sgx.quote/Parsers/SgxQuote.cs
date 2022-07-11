using Org.BouncyCastle.X509;
using System.Collections;
using System.Text;

namespace ParseSgxQuote
{
    // https://download.01.org/intel-sgx/latest/dcap-latest/linux/docs/Intel_SGX_ECDSA_QuoteLibReference_DCAP_API.pdf

    public class SgxQuote
    {
        const int quoteSignatureStart = 48 + 384 + 4;
        const int qeAuthenticationDataStart = 64 + 64 + 384 + 64 + quoteSignatureStart;

        private byte[] _quote;

        public SgxQuote(byte[] quote)
        {
            _quote = quote;

            if ((_quote[0] == 3) && (_quote[1] == 0))
            {
                QuoteType = SgxQuoteType.Intel;
            }
            else if ((_quote[16] == 3) && (_quote[17] == 0))
            {
                QuoteType = SgxQuoteType.OpenEnclave;
                _quote = _quote.Skip(16).ToArray();
            }

            if (QuoteType == SgxQuoteType.Unknown)
                throw new SgxQuoteException("Unknown quote type!  Neither Intel nor Open Enclave!");

            Header = new QuoteHeader
            {
                Version = BitConverter.ToInt16(_quote, 0),
                AttestationType = BitConverter.ToInt16(_quote, 2),
                QESVN = BitConverter.ToInt16(_quote, 8),
                PCESVN = BitConverter.ToInt16(_quote, 10),
                QEVendorID = _quote.Skip(12).Take(16).ToArray(),
                UserData = _quote.Skip(28).Take(20).ToArray()
            };
            Report = new ReportBody
            {
                CPUSVN = _quote.Skip(48).Take(16).ToArray(),
                MiscSelect = BitConverter.ToInt32(_quote, 64),
                Attributes = _quote.Skip(96).Take(16).ToArray(),
                IsDebuggable = (_quote[96] & 2) != 0,
                MrEnclave = _quote.Skip(112).Take(32).ToArray(),
                MrSigner = _quote.Skip(176).Take(32).ToArray(),
                ISVProdID = BitConverter.ToInt16(_quote, 304),
                ISVSVN = BitConverter.ToInt16(_quote, 306),
                ReportData = _quote.Skip(368).Take(64).ToArray()
            };

            PckCerts = new PckCertChain(GetPckCertChainPem());
        }

        public SgxQuoteType QuoteType { get; set; } = SgxQuoteType.Unknown;

        public QuoteHeader Header { get; set; } = new QuoteHeader();

        public ReportBody Report { get; set; } = new ReportBody();

        public PckCertChain PckCerts { get; set; } = new PckCertChain(string.Empty);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"");
            sb.AppendLine($"Intel Reference on SGX Quote Format: https://download.01.org/intel-sgx/latest/dcap-latest/linux/docs/Intel_SGX_ECDSA_QuoteLibReference_DCAP_API.pdf");
            sb.AppendLine($"Intel Reference on PCK Cert Format:  https://api.trustedservices.intel.com/documents/Intel_SGX_PCK_Certificate_CRL_Spec-1.4.pdf");
            sb.AppendLine($"");
            sb.AppendLine($"SGX Quote");
            sb.AppendLine($"");
            sb.AppendLine($"Quote Type      = {QuoteType}");
            sb.AppendLine($"");
            sb.AppendLine($"Header");
            sb.AppendLine($"");
            sb.AppendLine($"Version         = {Header.Version}");
            sb.AppendLine($"AttestationType = {Header.AttestationType}");
            sb.AppendLine($"QESVN           = {Header.QESVN}");
            sb.AppendLine($"PCESVN          = {Header.PCESVN}");
            sb.AppendLine($"QEVendorID      = 0x{Header.QEVendorID.ToHexString()}");
            sb.AppendLine($"UserData        = 0x{Header.UserData.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"Report Body");
            sb.AppendLine($"");
            sb.AppendLine($"CPUSVN          = 0x{Report.CPUSVN.ToHexString()}");
            sb.AppendLine($"MiscSelect      = {Report.MiscSelect}");
            sb.AppendLine($"Attributes      = 0x{Report.Attributes.ToHexString()}");
            sb.AppendLine($"IsDebuggable    = {Report.IsDebuggable}");
            sb.AppendLine($"MrEnclave       = 0x{Report.MrEnclave.ToHexString()}");
            sb.AppendLine($"MrSigner        = 0x{Report.MrSigner.ToHexString()}");
            sb.AppendLine($"ISVProdID       = {Report.ISVProdID}");
            sb.AppendLine($"ISVSVN          = {Report.ISVSVN}");
            sb.AppendLine($"ReportData      = 0x{Report.ReportData.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"PCK Certificate");
            sb.AppendLine($"");
            sb.AppendLine($"PPID            = 0x{PckCerts.PPID.ToHexString()}");
            sb.AppendLine($"TCBComp         = ({string.Join(", ", PckCerts.TCBComp)})");
            sb.AppendLine($"PCESVN          = {PckCerts.PCESVN}");
            sb.AppendLine($"CPUSVN          = 0x{PckCerts.CPUSVN.ToHexString()}");
            sb.AppendLine($"PCEID           = 0x{PckCerts.PCEID.ToHexString()}");
            sb.AppendLine($"FMSPC           = 0x{PckCerts.FMSPC.ToHexString()}");
            sb.AppendLine($"");

            return sb.ToString();
        }

        private string GetPckCertChainPem()
        {
            // Get to start of QE Certification Data
            var qeAuthenticationDataLength = BitConverter.ToInt16(_quote, qeAuthenticationDataStart);
            var qeCertificationDataStart = qeAuthenticationDataStart + qeAuthenticationDataLength + 2;

            // Ensure Certification Data type is 5
            var qeCertificationDataType = BitConverter.ToInt16(_quote, qeCertificationDataStart);
            if (qeCertificationDataType == 5)
            {
                var pckCertChainLength = BitConverter.ToInt32(_quote, qeCertificationDataStart + 2);
                var pckCertChainOffset = qeCertificationDataStart + 2 + 4;
                var pckCertChainBytes = new byte[pckCertChainLength];
                Array.Copy(_quote, pckCertChainOffset, pckCertChainBytes, 0, pckCertChainLength);
                return Encoding.UTF8.GetString(pckCertChainBytes);
            }
            else
            {
                throw new SgxQuoteException($"QE Certification Data Type == {qeCertificationDataType}.  Expected 5!");
            }
        }

        public enum SgxQuoteType
        {
            Unknown,
            OpenEnclave,
            Intel
        }

        public class QuoteHeader
        {
            public Int16 Version { get; set; }
            public Int16 AttestationType { get; set; }
            public Int16 QESVN { get; set; }
            public Int16 PCESVN { get; set; }
            public byte[] QEVendorID { get; set; } = new byte[0];
            public byte[] UserData { get; set; } = new byte[0];
        }

        public class ReportBody
        {
            public byte[] CPUSVN { get; set; } = new byte[0];
            public Int32 MiscSelect { get; set; }
            public byte[] Attributes { get; set; } = new byte[0];
            public bool IsDebuggable { get; set; }
            public byte[] MrEnclave { get; set; } = new byte[0];
            public byte[] MrSigner { get; set; } = new byte[0];
            public Int16 ISVProdID { get; set; }
            public Int16 ISVSVN { get; set; }
            public byte[] ReportData { get; set; } = new byte[0];
        }

        public class SgxQuoteException : Exception
        {
            public SgxQuoteException(string message) : base(message) { }
        }
    }
}