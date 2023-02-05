using maa.perf.test.core.Model;
using maa.perf.test.core.Utils;

namespace maa.perf.test.core.Maa.Preview
{
    public class AttestTeeOpenEnclaveRequestBody
    {
        public AttestTeeOpenEnclaveRequestBody(EnclaveInfo enclaveInfo)
        {
            Quote = HexHelper.ConvertHexToBase64Url(enclaveInfo.QuoteHex);
            EnclaveHeldData = HexHelper.ConvertHexToBase64Url(enclaveInfo.EnclaveHeldDataHex);
        }
        public string Quote { get; set; }
        public string EnclaveHeldData { get; set; }
    }
}