namespace maa.perf.test.core.Maa.Preview
{
    using maa.perf.test.core.Model;
    using maa.perf.test.core.Utils;

    public class AttestTeeOpenEnclaveRequestBody
    {
        public AttestTeeOpenEnclaveRequestBody(EnclaveInfo enclaveInfo)
        {
            Quote = enclaveInfo.Quote;
            EnclaveHeldData = enclaveInfo.EnclaveHeldData;
        }
        public string Quote { get; set; }
        public string EnclaveHeldData { get; set; }
    }
}