using maa.perf.test.core.Utils;

namespace maa.perf.test.core.Maa
{
    public class EnclaveInfo
    {
        public int Type { get; set; }
        public string MrEnclaveHex { get; set; }
        public string MrSignerHex { get; set; }
        public string ProductIdHex { get; set; }
        public uint SecurityVersion { get; set; }
        public ulong Attributes { get; set; }
        public string QuoteHex { get; set; }
        public string EnclaveHeldDataHex { get; set; }

        public static EnclaveInfo CreateFromFile(string filePath)
        {
            return SerializationHelper.ReadFromFile<EnclaveInfo>(filePath);
        }
    }
}
