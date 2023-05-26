using Microsoft.Extensions.Caching.Memory;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession
{
    public record ActiveSessionOptions
    {
        public String HostId { get; set; } = DEFAULT_HOST_NAME;
        public String Prefix { get; set; } = DEFAULT_SESSION_KEY_PREFIX;
        public TimeSpan MaxLifetime { get; set; } = DEFAULT_MAX_LIFETIME;
        public Boolean UseOwnCache { get; set;}
        public MemoryCacheOptions? OwnCacheOptions { get; set; } 
        public Boolean ThrowOnRemoteRunner { get; set; } = true;
        public Boolean CacheRunnerAsTask { get; set; } 

    }
}

/*
 * Configuration for ActiveSession feature
 * Section key: value of CONFIG_KEY_NAME in the IConfiguration root
 * Keys in the section:
 *   HostId (string) - this host identifier
 *   Prefix (string) - prefix for all keys of key-value pairs in the Session store for the ActiveSession variables
 *   UseOwnCache(Boolean) - flag to use own (not shared) MemoryCache instance as a storage
 *   ThrowOnRemoteRunner(Boolean) - Throw exception if the runner is found on thr remote server
 *   CacheRunnerAsTask - Store runners in the cache as completed tasks to speed up async calls
 *   OwnCaheOptions(nested of type MemoryCacheOptions) - options used to set up own MemoryCache instance (ignored for shared cache)
 */ 