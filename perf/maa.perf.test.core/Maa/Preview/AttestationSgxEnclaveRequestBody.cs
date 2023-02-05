namespace maa.perf.test.core.Maa.Preview
{
    using maa.perf.test.core.Model;
    using maa.perf.test.core.Utils;
    using System.Linq;

    public class AttestSgxEnclaveRequestBody
    {
        public AttestSgxEnclaveRequestBody(EnclaveInfo enclaveInfo)
        {
            Quote = Base64Url.EncodeBytes(Base64Url.DecodeBytes(enclaveInfo.Quote).Skip(16).ToArray());
            EnclaveHeldData = enclaveInfo.EnclaveHeldData;
        }
        public string Quote { get; set; }
        public string EnclaveHeldData { get; set; }
    }

}