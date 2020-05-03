using System;

namespace validatequotes
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

        public AttestOpenEnclaveRequestBody GetMaaBody()
        {
            var maaBody = new AttestOpenEnclaveRequestBody
            {
                Quote = HexHelper.ConvertHexToBase64Url(QuoteHex),
                EnclaveHeldData = HexHelper.ConvertHexToBase64Url(EnclaveHeldDataHex)
            };
            return maaBody;
        }

        public void CompareToMaaServiceJwtToken(string serviceJwtToken, bool includeDetails)
        {
            var jwtBody = JoseHelper.ExtractJosePart(serviceJwtToken, 1);

            if (includeDetails)
            {
                Logger.WriteLine("");
                Logger.WriteLine("Claims in MAA Service JWT Token");
                foreach (var p in jwtBody)
                {
                    Logger.WriteLine($"{p.Key} : {p.Value}");
                }
            }

            var isDebuggable = (Attributes & 1) == 1;
            var isd = jwtBody["is-debuggable"];
            var isdpassed = isDebuggable == (bool)isd;
            Logger.WriteLine($"IsDebuggable check:      {isdpassed}");
            if (includeDetails)
            {
                Logger.WriteLine($"    We think     {isDebuggable}");
                Logger.WriteLine($"    MAA service: {isd}");
            }

            var mre = jwtBody["sgx-mrenclave"];
            var mrepassed = MrEnclaveHex.ToLower().Equals((string)mre);
            Logger.WriteLine($"MRENCLAVE check:         {mrepassed}");
            if (includeDetails)
            {
                Logger.WriteLine($"    We think:    {MrEnclaveHex.ToLower()}");
                Logger.WriteLine($"    MAA service: {mre}");
            }

            var mrs = jwtBody["sgx-mrsigner"];
            var mrspassed = MrSignerHex.ToLower().Equals(((string)mrs).ToLower());
            Logger.WriteLine($"MRSIGNER check:          {mrspassed}");
            if (includeDetails)
            {
                Logger.WriteLine($"    We think     {MrSignerHex.ToLower()}");
                Logger.WriteLine($"    MAA service: {mrs}");
            }

            var pid = jwtBody["product-id"];
            var pidpassed = BitConverter.ToUInt64(HexHelper.ConvertHexToByteArray(ProductIdHex), 0) == (ulong)pid;
            Logger.WriteLine($"ProductID check:         {pidpassed}");
            if (includeDetails)
            {
                Logger.WriteLine($"    We think     {BitConverter.ToUInt64(HexHelper.ConvertHexToByteArray(ProductIdHex), 0)}");
                Logger.WriteLine($"    MAA service: {pid}");
            }

            var svn = jwtBody["svn"];
            var svnPassed = SecurityVersion == (uint)svn;
            Logger.WriteLine($"Security Version check:  {svnPassed}");
            if (includeDetails)
            {
                Logger.WriteLine($"    We think     {SecurityVersion}");
                Logger.WriteLine($"    MAA service: {svn}");
            }

            var ehd = jwtBody["maa-ehd"];
            var ehdPassed = HexHelper.ConvertHexToBase64Url(EnclaveHeldDataHex).Equals((string)ehd);
            Logger.WriteLine($"Enclave Held Data check: {ehdPassed}");
            if (includeDetails)
            {
                Logger.WriteLine($"    We think     {HexHelper.ConvertHexToBase64Url(EnclaveHeldDataHex)}");
                Logger.WriteLine($"    MAA service: {ehd}");
            }

            Logger.WriteLine("");
        }
    }
}
