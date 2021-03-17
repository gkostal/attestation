using maa.perf.test.core.Utils;
using Newtonsoft.Json;

namespace maa.perf.test.core.Model
{
    public class PerformanceInformation
    {
        public MachineInformation Machine { get; set; } = new MachineInformation();

        public CpuInformation Cpu { get; set; } = new CpuInformation();

        public MemoryInformation Memory { get; set; } = new MemoryInformation();

        public EnclaveInformation Enclave { get; set; } = new EnclaveInformation();

        public RequestInformation Request { get; set; } = new RequestInformation();

        public static PerformanceInformation CreateFromHeaderString(string headerValue)
        {
            var jsonValue = Base64Url.DecodeString(headerValue);
            return JsonConvert.DeserializeObject<PerformanceInformation>(jsonValue);
        }

        public string ExportToHeaderString()
        {
            var jsonValue = JsonConvert.SerializeObject(this);
            return Base64Url.EncodeString(jsonValue);
        }

        public class MachineInformation
        {
            public CpuInformation Cpu { get; set; }

            public MemoryInformation Memory { get; set; }

            public EnclaveInformation Enclave { get; set; }
        }

        public class CpuInformation
        {
            public float Total { get; set; }

            public float Privileged { get; set; }

            public float User { get; set; }

            public float AttestationRp { get; set; }

            public float AttestationTenant { get; set; }

            public float EnclaveHost { get; set; }
        }

        public class MemoryInformation
        {
            public float Total { get; set; }

            public float AttestationRp { get; set; }

            public float AttestationTenant { get; set; }

            public float EnclaveHost { get; set; }
        }

        public class EnclaveInformation
        {
            public int CurrentActiveThreads { get; set; }

            public float EpcMemoryConsumed { get; set; }
        }

        public class RequestInformation
        {
            public long DurationMs { get; set; }

            public long NumberOffBoxCalls { get; set; }
        }
    }
}
