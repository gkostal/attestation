namespace maa.perf.test.core.Maa.Both
{
    using maa.perf.test.core.Utils;

    public class AttestAzureGuestRequestBody
    {
        public AttestAzureGuestRequestBody(string attestationInfo = null)
        {
            AttestationInfo = attestationInfo;
        }

        public string AttestationInfo { get; set; }
    }
}