namespace maa.perf.test.core.Model
{
    using maa.perf.test.core.Utils;

    public class EnclaveInfo
    {
        public string Quote { get; set; }
        public string EnclaveHeldData { get; set; }

        public static EnclaveInfo CreateFromFile(string filePath)
        {
            return SerializationHelper.ReadFromFileCached<EnclaveInfo>(filePath);
        }
    }
}
