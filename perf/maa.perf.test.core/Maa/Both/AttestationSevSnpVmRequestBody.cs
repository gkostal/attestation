namespace maa.perf.test.core.Maa.Both
{
    using maa.perf.test.core.Utils;

    public class AttestSevSnpVmRequestBody
    {
        public class AttestedData
        {
            public string Data { get; set; }
            public string DataType { get; set; }
        }

        public AttestSevSnpVmRequestBody()
        {
        }

        public AttestSevSnpVmRequestBody(string report, string attestedDataBase64Url, string attestedDataType)
        {
            Report = report;
            RuntimeData = new AttestedData()
            {
                Data = attestedDataBase64Url,
                DataType = attestedDataType
            };
        }

        public string Report { get; set; }

        public AttestedData RuntimeData { get; set; }

        public string DraftPolicyForAttestation { get; set; }

        public static AttestSevSnpVmRequestBody CreateFromFile(string filePath)
        {
            return SerializationHelper.ReadFromFileCached<AttestSevSnpVmRequestBody>(filePath);
        }
    }

}