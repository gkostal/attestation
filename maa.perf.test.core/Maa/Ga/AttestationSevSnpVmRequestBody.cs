using maa.perf.test.core.Model;
using maa.perf.test.core.Utils;
using System.Runtime.Serialization;

namespace maa.perf.test.core.Maa.Ga
{
    public class AttestSevSnpRequestBody
    {
        public AttestSevSnpRequestBody()
        { 
        }

        public AttestSevSnpRequestBody(string reportType, string report)
        {
            ReportType = reportType;
            Report = report;
        }

        public string ReportType { get; set; }

        public string Report { get; set; }

        public string DraftPolicyForAttestation { get; set; }

        public static AttestSevSnpRequestBody CreateFromFile(string filePath)
        {
            return SerializationHelper.ReadFromFile<AttestSevSnpRequestBody>(filePath);
        }
    }

}