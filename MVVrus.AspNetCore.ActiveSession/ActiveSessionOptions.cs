using Microsoft.Extensions.Caching.Memory;

namespace MVVrus.AspNetCore.ActiveSession
{
    public class ActiveSessionOptions
    {
        public const String CONFIG_KEY_NAME = "MVVrus.ActiveSessions";
        public String? HostId { get; set; } = "localhost";
        public String? Prefix { get; set; } = "##ActiveSession##";
        public TimeSpan? MaxLifetime { get; set; } = DEFAULT_MAX_LIFETIME;
        public Boolean? UseOwnCache { get; set; } = false;
        public MemoryCacheOptions? OwnCacheOptions { get; set; } = null;
        public Boolean ThrowOnRemoteRunner { get; set; } = true;

        public static readonly TimeSpan DEFAULT_MAX_LIFETIME = TimeSpan.FromHours(2);
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
 *   OwnCaheOptions(nested of type MemoryCacheOptions) - options used to set up own MemoryCache instance (ignored for shared cache)
 */ 