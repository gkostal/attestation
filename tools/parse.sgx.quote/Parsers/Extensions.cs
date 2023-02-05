using Org.BouncyCastle.Asn1;

namespace ParseSgxQuote
{
    public static class Extensions
    {
        public static Asn1Object XlatToAsn1Object(this Asn1OctetString x)
        {
            var octetStream = new Asn1InputStream(x.GetOctetStream());
            return octetStream.ReadObject();
        }

        public static string ToHexString(this byte[] x)
        {
            return BitConverter.ToString(x).Replace("-", "").ToLower();
        }
    }
}
