namespace maa.perf.test.core.Maa.Ga
{
    using maa.perf.test.core.Model;
    using maa.perf.test.core.Utils;

    public class AttestOpenEnclaveRequestBody
    {
        public class AttestedData
        {
            public string Data { get; set; }
            public string DataType { get; set; }
        }

        public AttestOpenEnclaveRequestBody(EnclaveInfo enclaveInfo)
        {
            Report = enclaveInfo.Quote;
            RuntimeData = new AttestedData()
            {
                Data = enclaveInfo.EnclaveHeldData,
                DataType = "Binary"
            };
        }

        public string Report { get; set; }
        public AttestedData RuntimeData { get; set; }
        public AttestedData InittimeData { get; set; }
        public string DraftPolicyForAttestation { get; set; }
    }

}