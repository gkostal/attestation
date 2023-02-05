namespace maa.perf.test.core.Maa.Ga
{
    using maa.perf.test.core.Model;
    using maa.perf.test.core.Utils;
    using System.Linq;

    public class AttestSgxEnclaveRequestBody
    {
        public class AttestedData
        {
            public string Data { get; set; }
            public string DataType { get; set; }
        }

        public AttestSgxEnclaveRequestBody(EnclaveInfo enclaveInfo)
        {
            Quote = Base64Url.EncodeBytes(Base64Url.DecodeBytes(enclaveInfo.Quote).Skip(16).ToArray());
            RuntimeData = new AttestedData()
            {
                Data = enclaveInfo.EnclaveHeldData,
                DataType = "Binary"
            };
        }

        public string Quote { get; set; }
        public AttestedData RuntimeData { get; set; }
        public AttestedData InittimeData { get; set; }
        public string DraftPolicyForAttestation { get; set; }
    }

}