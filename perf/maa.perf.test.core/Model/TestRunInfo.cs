namespace maa.perf.test.core.Model
{
    public class TestRunInfo
    {
        public TestRunInfo()
        {
            SimultaneousConnections = 5;
            SimultaneousConnectionsDelta = -1;
            SimultaneousConnectionsMaxConnections = -1;
            TargetRPS = 1;
            ForceReconnects = false;
            MeasureServerSideTime = false;
            RampUpTimeSeconds = 0;
            TestTimeSeconds = int.MaxValue;
            EnclaveInfoFile = "./Quotes/enclave.info.release.json";
        }
        public long SimultaneousConnections { get; set; }

        public long SimultaneousConnectionsDelta { get; set; }

        public long SimultaneousConnectionsMaxConnections { get; set; }

        public double TargetRPS { get; set; }

        public bool ForceReconnects { get; set; }

        public bool MeasureServerSideTime { get; set; }

        public int RampUpTimeSeconds { get; set; }

        public int TestTimeSeconds { get; set; }

        public string EnclaveInfoFile { get; set; }
    }
}
