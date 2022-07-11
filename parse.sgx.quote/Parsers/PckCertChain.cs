using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.X509;
using System.Text;

namespace ParseSgxQuote
{
    public class PckCertChain
    {
        private string _certChain;
        private List<X509Certificate> _certs;

        public PckCertChain(string certChain)
        {
            _certChain = certChain;

            var c = new X509CertificateParser().ReadCertificates(Encoding.UTF8.GetBytes(certChain));
            _certs = new List<X509Certificate>(c.Count);
            foreach (var cert in c)
                _certs.Add((X509Certificate)cert);
        }

        public override string ToString()
        {
            //return _certChain;
            return _certs[0].ToString();
        }

        public byte[] PPID => (GetExtensionOidValue(_certs[0], "1.2.840.113741.1.13.1.1") as DerOctetString).GetOctets();

        public long[] TCBComp { get
            {
                List<long> l = new List<long>();

                for (int i = 1; i < 17; i++)
                {
                    var oid = $"1.2.840.113741.1.13.1.2.{i}";
                    var y = GetExtensionOidValue(_certs[0], oid) as DerInteger;
                    l.Add(y.LongValueExact);
                }

                return l.ToArray();
            }
        }

        public long PCESVN => (GetExtensionOidValue(_certs[0], "1.2.840.113741.1.13.1.2.17") as DerInteger).LongValueExact;

        public byte[] CPUSVN => (GetExtensionOidValue(_certs[0], "1.2.840.113741.1.13.1.2.18") as DerOctetString).GetOctets();

        public byte[] PCEID => (GetExtensionOidValue(_certs[0], "1.2.840.113741.1.13.1.3") as DerOctetString).GetOctets();

        public byte[] FMSPC => (GetExtensionOidValue(_certs[0], "1.2.840.113741.1.13.1.4") as DerOctetString).GetOctets();

        private List<Asn1Object> GetTopLevelExtensionObjects(X509Certificate c)
        {
            var oidStrings = new List<string>();

            foreach (var a in c.GetCriticalExtensionOids())
                oidStrings.Add(a.ToString());

            foreach (var a in c.GetNonCriticalExtensionOids())
                oidStrings.Add(a.ToString());

            return oidStrings.ConvertAll(s => c.GetExtensionValue(s).XlatToAsn1Object());
        }

        private Asn1Object GetExtensionOidValue(X509Certificate cert, string oid)
        {
            var result = default(Asn1Object);
            var topLevelExtensionObjects = GetTopLevelExtensionObjects(cert);

            for (int i = 0; i < topLevelExtensionObjects.Count && result == default(Asn1Object); i++)
            {
                result = GetExtensionOidValue(topLevelExtensionObjects[i], oid);
            }

            return result;
        }

        private Asn1Object GetExtensionOidValue(Asn1Object o, string oid)
        {
            var result = default(Asn1Object);

            if (o is Asn1Sequence os)
            {
                if ((os.Count == 2) && (os[0].ToString() == oid))
                {
                    result = os[1].ToAsn1Object();
                }
                else
                {
                    for (int i = 0; i < os.Count && result == default(Asn1Object); i++)
                    {
                        result = GetExtensionOidValue(os[i].ToAsn1Object(), oid);
                    }
                }
            }

            return result;
        }
    }
}
