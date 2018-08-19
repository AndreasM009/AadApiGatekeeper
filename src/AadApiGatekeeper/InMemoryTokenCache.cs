using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.AspNetCore.Authentication
{
    public class MemoryTokenCache : TokenCache
    {
        private static readonly object _syncRoot = new object();
        private string _userObjectId = string.Empty;
        private string _cacheId = string.Empty;
        private static IMemoryCache _cache;

        static MemoryTokenCache()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public MemoryTokenCache(string userId, string preafix)
        {
            _userObjectId = userId;
            _cacheId = $"{_userObjectId}_{preafix}_TokenCache";
            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            Load();
        }

        public void Load()
        {
            lock (_syncRoot)
            {
                this.Deserialize(_cache.Get<byte[]>(_cacheId));
            }
        }

        public void Persist()
        {
            lock (_syncRoot)
            {
                // reflect changes in the persistent store
                _cache.Set(_cacheId, this.Serialize());
                // once the write operation took place, restore the HasStateChanged bit to false
                this.HasStateChanged = false;
            }
        }

        // Empties the persistent store.
        public override void Clear()
        {
            base.Clear();
            _cache.Remove(_cacheId);
        }

        public override void DeleteItem(TokenCacheItem item)
        {
            base.DeleteItem(item);
            Persist();
        }

        // Triggered right before ADAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
        }

        // Triggered right after ADAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (this.HasStateChanged)
            {
                Persist();
            }
        }
    }
}