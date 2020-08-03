using System.IO;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace maa.perf.test.core.Utils
{
    internal class PersistentTokenCache : TokenCache
    {
        public static PersistentTokenCache Instance = new PersistentTokenCache();

        private PersistentTokenCache()
        {
            AfterAccess = AfterAccessNotification;
            Deserialize(File.Exists(_persistentCacheFilePath) ? File.ReadAllBytes(_persistentCacheFilePath) : null);
        }

        public override void Clear()
        {
            base.Clear();
            File.Delete(_persistentCacheFilePath);
        }

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (HasStateChanged)
            {
                File.WriteAllBytes(_persistentCacheFilePath, Serialize());
                HasStateChanged = false;
            }
        }

        private readonly string _persistentCacheFilePath = @".\PersistentTokenCache.dat";
    }
}