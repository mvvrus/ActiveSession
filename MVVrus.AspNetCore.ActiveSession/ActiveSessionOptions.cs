﻿using Microsoft.Extensions.Caching.Memory;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The object containing options for configuring an ActiveSession feature
    /// </summary>
    /// <remarks>
    /// This record structure mimics the structure of the configuration section for the ActiveSession feature
    /// It has properties with the same name as keys in the configuration section
    /// So it documents the configuration section
    /// The name of the configuration section itself is "MVVrus.ActiveSessions" (a value of the CONFIG_SECTION_NAME constant)
    /// </remarks>
    public record ActiveSessionOptions
    {
        /// <value>This host identifier.</value>
        /// <remarks>
        /// It is used only for multi-instance active sessions (currently not implemented)
        /// and must be unique for each application instanse only in this case.
        /// </remarks>
        public String HostId { get; set; } = DEFAULT_HOST_NAME;

        /// <value>Prefix for all keys of ActiveSession variables in the ISession store</value>
        public String Prefix { get; set; } = DEFAULT_SESSION_KEY_PREFIX;

        /// <value>Maximum lifetime for ActiveSession objects (measured from a session creation time) </value>
        public TimeSpan MaxLifetime { get; set; } = DEFAULT_MAX_LIFETIME;

        /// <value>The flag to use own (not shared) MemoryCache instance as an ActiveSession objects storage</value>
        public Boolean UseOwnCache { get; set;}

        /// <value>Options used to set up own MemoryCache instance (ignored for shared cache)</value>
        public MemoryCacheOptions? OwnCacheOptions { get; set; }

        /// <value>Throw exception if the runner is running by the remote application instance</value>
        public Boolean ThrowOnRemoteRunner { get; set; } = true;

        /// <value>Store runners in the cache as completed task obects to speed up async calls</value>
        public Boolean CacheRunnerAsTask { get; set; } 

        ///<value>Replace value of RequestServices in HttpContext by the value of the ActiveSession.SessionServices</value>
        public Boolean UseSessionServicesAsRequestServices { get; set; }

        /// <value> Track storage statistics</value>
        public Boolean TrackStatistics { get; set; }

        /// <value> Dispose a session evicted from the cache synchronously </value>
        public Boolean WaitForEvictedSessionDisposal { get; set; }
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