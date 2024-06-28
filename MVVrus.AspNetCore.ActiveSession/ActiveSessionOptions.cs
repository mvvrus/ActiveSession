using Microsoft.Extensions.Caching.Memory;
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
        /// <summary>This host identifier.</summary>
        /// <remarks>
        /// It is used only for multi-instance active sessions (currently not implemented)
        /// and must be unique for each application instanse only in this case.
        /// </remarks>
        public String HostId { get; set; } = DEFAULT_HOST_NAME;

        /// <summary>Prefix for all keys of ActiveSession variables in the ISession store</summary>
        public String Prefix { get; set; } = DEFAULT_SESSION_KEY_PREFIX;

        /// <summary>Maximum idle time for runner objects  </summary>
        public TimeSpan RunnerIdleTimeout { get; set; } = DEFAULT_RUNNER_IDLE_TIMEOUT;

        /// <summary>Maximum lifetime for ActiveSession objects (measured from a session creation time) </summary>
        public TimeSpan MaxLifetime { get; set; } = DEFAULT_MAX_LIFETIME;

        /// <summary>The flag to use own (not shared) MemoryCache instance as an ActiveSession objects storage</summary>
        public Boolean UseOwnCache { get; set;}

        /// <summary>Throw exception if the runner is running by the remote application instance</summary>
        public Boolean ThrowOnRemoteRunner { get; set; } = true;

        /// <summary>Options used to set up own MemoryCache instance (ignored for shared cache)</summary>
        public MemoryCacheOptions? OwnCacheOptions { get; set; }

        ///<summary>Replace value of RequestServices in HttpContext by the value of the ActiveSession.SessionServices</summary>
        public Boolean UseSessionServicesAsRequestServices { get; set; }

        /// <summary> Track storage statistics</summary>
        public Boolean TrackStatistics { get; set; }

        /// <summary> Default size of an IActiveSession-implementing  object</summary>
        public Int32 ActiveSessionSize { get; set; } = DEFAULT_ACTIVESESSIONSIZE;

        /// <summary> Default size of an IRunner-implementing  object</summary>
        public Int32 DefaultRunnerSize { get; set; } = DEFAULT_RUNNERSIZE;

        /// <summary> Timeout for logging runner cleanup outcome</summary>
        public Int32? CleanupLoggingTimeoutMs { get; set; }

        /// <summary> 
        /// Asynchronously preload IActiveSessionFeature.ActiveSession before processing the rest of the pipeline
        /// </summary>
        public Boolean PreloadActiveSession { get; set; } = true;

        /// <summary>
        /// Timeout for processing HTTP request path string by a middleware Regex-based filter
        /// </summary>
        public TimeSpan PathRegexTimeout { get; internal set; } = DEFAULT_PATHREGEXTIMEOUT;

        /// <summary>
        /// Default number of items to fetch by GetRequireAsync method of runners, returning an IEnumerable{TItem} result
        /// </summary>
        public Int32 DefaultEnumerableAdvance { get; set; } = ENUM_DEFAULT_ADVANCE;

        /// <summary>
        /// Default size of a queue used by GetRequireAsync method of runners, returning an IEnumerable{TItem} result
        /// </summary>
        public Int32 DefaultEnumerableQueueSize { get; set; } = ENUM_DEFAULT_QUEUE_SIZE;
    }
}

/*
 * Configuration for ActiveSession feature
 * Section key: value of CONFIG_KEY_NAME in the IConfiguration root
 * Keys in the section:
 *   HostId (string) - this host identifier
 *   Prefix (string) - prefix for all keys of key-value pairs in the Session store for the ActiveSession variables
 *   UseOwnCache(Boolean) - flag to use own (not shared) MemoryCache instance as a storage
 *   MaxLifetime(TimeSpan) - maximum lifetime for ActiveSession objects
 *   ThrowOnRemoteRunner(Boolean) - Throw exception if the runner is found on thr remote server
 *   CacheRunnerAsTask - Store runners in the cache as completed tasks to speed up async calls
 *   OwnCaheOptions(nested of type MemoryCacheOptions) - options used to set up own MemoryCache instance (ignored for shared cache)
 *   UseSessionServicesAsRequestServices(Boolean) - [experimental]set HttpContext.RequestServices property to contain ActiveSession.SessionServices
 *   TrackStatistics(Boolean) - perfom tracking of cache using statistics by an IActiveSessionStore-implementing class
 *   ActiveSessionSize - size of an IActiveSession-implementing object in a cache(arbitrary units, defaults to 1)
 *   DefaultRunnerSize - default size of an IRunner-implementing object in a cache(arbitrary units, defaults to 1)
 *   CleanupLoggingTimeoutMs - timeout to log an outcome of runers cleanup(msec, defaults to null: no outcome logging)
 */ 