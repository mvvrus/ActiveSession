using Microsoft.Extensions.Caching.Memory;
using static Microsoft.Extensions.Logging.LogLevel;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal static partial class LoggingExtensions
    {
        [LoggerMessage(1, Error, "The exception occured while creating the ActiveSession middleware.")]
        public static partial void LogErrorMiddlewareCannotBeCreated(this ILogger Logger, Exception AnException);

        [LoggerMessage(2, Error, "The exception occured while the rest of the pipeline, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogErrorPipelineException(this ILogger Logger, Exception AnException, String TraceIdentifier);

        [LoggerMessage(3, Error, "Shared cache to be used is not available")]
        public static partial void LogErrorNoSharedCacheException(this ILogger Logger);


        [LoggerMessage(1000, Warning, "Cannot obtain an ActiveSession store service from the application service container. The ActiveSession middleware cannot be registered. Was the IServiceCollection.AddActiveServices extension method called at least once?")]
        public static partial void LogWarningAbsentFactoryInplementations(this ILogger Logger, Exception AnException);

        [LoggerMessage(1100, Warning, "ActiveSessionStore constructor: cannot create our own cache, fall back to the shared cache.")]
        public static partial void LogWarningActiveSessionStoreCannotCreateOwnCache(this ILogger Logger, Exception AnException);


        [LoggerMessage(3000, Debug, "ActiveSession middleware is registered for addition to a middleware pipeline.")]
        public static partial void LogDebugActiveSessionMiddlewareRegistered(this ILogger Logger);

        [LoggerMessage(3001, Debug, "ActiveSession middleware is added to the middleware pipeline.")]
        public static partial void LogDebugActiveSessionMiddlewareAdded(this ILogger Logger);

        [LoggerMessage(3002, Debug, "ActiveSession middleware is invoked, TraceIdentifier=\"{TraceIdentifier}\", Session.Id=\"{SessionId}\".")]
        public static partial void LogDebugActiveSessionMiddlewareInvoked(this ILogger Logger, String TraceIdentifier, String SessionId);

        [LoggerMessage(3100, Debug, "ActiveSessionStore constructor will use the follwing options: {Options}.")]
        public static partial void LogDebugActiveSessionStoreConstructorOptions(this ILogger Logger, ActiveSessionOptions Options);

        [LoggerMessage(3101, Debug, "ActiveSessionStore constructor will use the follwing options for its own cache: {Options}.")]
        public static partial void LogDebugActiveSessionStoreConstructorOwnCacheOptions(this ILogger Logger, MemoryCacheOptionsForLogging Options);

        [LoggerMessage(3102, Debug, "ActiveSessionStore constructor created its own cache.")]
        public static partial void LogDebugActiveSessionStoreConstructorOwnCaheCreated(this ILogger Logger);

        [LoggerMessage(3120, Debug, "ActiveSession key to use: \"{Key}\"")]
        public static partial void LogDebugActiveSessionKeyToUse(this ILogger Logger, String Key);

        [LoggerMessage(3121, Debug, "Found existing ActiveSession for the key: \"{Key}\"")]
        public static partial void LogDebugFoundExistingActiveSession(this ILogger Logger, String Key);

        [LoggerMessage(3122, Debug, "Creating new ActiveSession for the key: \"{Key}\"")]
        public static partial void LogDebugCreateNewActiveSession(this ILogger Logger, String Key);

        [LoggerMessage(3199, Debug, "")]
        public static partial void LogDebug99(this ILogger Logger);


        [LoggerMessage(4000, Trace, "Enetering ActiveSessionBuilderExtensions.UseActiveSessions.")]
        public static partial void LogTraceUseActiveSessions(this ILogger Logger);

        [LoggerMessage(4001, Trace, "Exiting ActiveSessionBuilderExtensions.UseActiveSessions.")]

        public static partial void LogTraceUseActiveSessionsExit(this ILogger Logger);
        [LoggerMessage(4002, Trace, "Enetering ActiveSessionMiddleware constructor.")]
        public static partial void LogTraceConstructActiveSessionMiddleware(this ILogger Logger);

        [LoggerMessage(4003, Trace, "Exiting ActiveSessionMiddleware constructor.")]
        public static partial void LogTraceConstructActiveSessionMiddlewareExit(this ILogger Logger);

        [LoggerMessage(4010, Trace, "Entering ActiveSessionMiddleware, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceInvokeActiveSessionMiddleware(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4011, Trace, "A feature object is created, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareFeatureCreated(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4012, Trace, "Invocing the rest of the middleware pipeline after the ActiveSession middleware, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareInvokeRest(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4013, Trace, "Control from the rest of the pipeline was returned without exceptions, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareControlReturns(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4014, Trace, "Cleaning up the feature object, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareCleanupStart(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4015, Trace, "ActiveSession middleware exited, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4100, Trace, "The ActiveSessionStore constructor entered.")]
        public static partial void LogTraceActiveSessionStoreConstructor(this ILogger Logger);

        [LoggerMessage(4101, Trace, "ActiveSessionStore constructor before create its own cache")]
        public static partial void LogTraceLogTraceActiveSessionStoreConstructorCreatingOwnCache(this ILogger Logger);

        [LoggerMessage(4102, Trace, "ActiveSessionStore constructor use shared cache")]
        public static partial void LogTraceActiveSessionStoreConstructorUseSharedCache(this ILogger Logger);

        [LoggerMessage(4103, Trace, "The ActiveSessionStore constructor exited.")]
        public static partial void LogTraceActiveSessionStoreConstructorExit(this ILogger Logger);

        [LoggerMessage(4110, Trace, "Disposing ActiveSessionStore object")]
        public static partial void LogTraceActiveSessionStoreDisposing(this ILogger Logger);

        [LoggerMessage(4120, Trace, "Enetering ActiveSessionStore.FetchOrCreate")]
        public static partial void LogTraceFetchOrCreate(this ILogger Logger);

        [LoggerMessage(4121, Trace, "Add a cache entry")]
        public static partial void LogTraceAddCacheEntry(this ILogger Logger);

        [LoggerMessage(4122, Trace, "Create a new ActiveSession and set it as the cache entry value")]
        public static partial void LogTraceCreateActiveSessionObject(this ILogger Logger);

        [LoggerMessage(4123, Trace, "//TODO LogTrace exiting FetchOrCreate")]
        public static partial void LogTraceFetchOrCreateExit(this ILogger Logger);

        [LoggerMessage(4199, Trace, "")]
        public static partial void LogTrace99(this ILogger Logger);

        public record struct MemoryCacheOptionsForLogging
        {
            public TimeSpan ExpirationScanFrequency { get; set; }
            public long? SizeLimit { get; set; }
            public double CompactionPercentage { get; set; }

            public MemoryCacheOptionsForLogging(MemoryCacheOptions Source)
            {
                this.ExpirationScanFrequency=Source.ExpirationScanFrequency;
                this.SizeLimit=Source.SizeLimit;
                this.CompactionPercentage=Source.CompactionPercentage;
            }
        }
    }
}
