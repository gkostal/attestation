using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maa.perf.test.core.Model
{
    public class TestRunInfo
    {
        public TestRunInfo()
        {
            SimultaneousConnections = 5;
            TargetRPS = 1;
            ForceReconnects = false;
            RampUpTimeSeconds = 0;
            TestTimeSeconds = int.MaxValue;
            EnclaveInfoFile = "./Quotes/enclave.info.release.json";
        }
        public long SimultaneousConnections { get; set; }

        public double TargetRPS { get; set; }

        public bool ForceReconnects { get; set; }

        public int RampUpTimeSeconds { get; set; }

        public int TestTimeSeconds { get; set; }

        public string EnclaveInfoFile { get; set; }
    }
}
