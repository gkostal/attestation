using maa.perf.test.core.Model;
using maa.perf.test.core.Utils;

namespace maa.perf.test.core.Maa.Ga
{
    public class AttestSgxEnclaveRequestBody
    {
        public class AttestedData
        {
            public string Data { get; set; }
            public string DataType { get; set; }
        }

        public AttestSgxEnclaveRequestBody(EnclaveInfo enclaveInfo)
        {
            Quote = HexHelper.ConvertHexToBase64Url(enclaveInfo.QuoteHex, 16);
            RuntimeData = new AttestedData()
            {
                Data = HexHelper.ConvertHexToBase64Url(enclaveInfo.EnclaveHeldDataHex),
                DataType = "Binary"
            };
        }

        public string Quote { get; set; }
        public AttestedData RuntimeData { get; set; }
        public AttestedData InittimeData { get; set; }
        public string DraftPolicyForAttestation { get; set; }
    }

}