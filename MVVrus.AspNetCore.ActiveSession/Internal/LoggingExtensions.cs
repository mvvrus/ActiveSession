﻿using Microsoft.Extensions.Caching.Memory;
using System;
using static Microsoft.Extensions.Logging.LogLevel;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal static partial class LoggingExtensions
    {
        [LoggerMessage(1, Error, "The exception occured while creating the ActiveSession middleware.")]
        public static partial void LogErrorMiddlewareCannotBeCreated(this ILogger Logger, Exception AnException);

        [LoggerMessage(2, Error, "Shared cache to be used is not available")]
        public static partial void LogErrorNoSharedCacheException(this ILogger Logger);

        [LoggerMessage(3, Error, "The factory failed to create a runner and returned null, TraceIdentifier=\"{TraceIdentifier}\". ")]
        public static partial void LogErrorCreateRunnerFailure(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4, Error, "Using remote runners is not allowed by configuration setting ThrowOnRemoteRunner, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogErrorRemoteRunnerUnavailable(this ILogger Logger, String TraceIdentifier);


        [LoggerMessage(1000, Warning, "Cannot obtain an ActiveSession store service from the application service container. The ActiveSession middleware cannot be registered. Was the IServiceCollection.AddActiveServices extension method called at least once?")]
        public static partial void LogWarningAbsentFactoryInplementations(this ILogger Logger, Exception AnException);

        [LoggerMessage(1100, Warning, "ActiveSessionStore constructor: cannot create our own cache, fall back to the shared cache.")]
        public static partial void LogWarningActiveSessionStoreCannotCreateOwnCache(this ILogger Logger, Exception AnException);

        [LoggerMessage(1160, Warning, "The runner is expected to be in the local cache but cannot be returned: no runner in the cache or the runner type is uncompatible, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogWarningNoExpectedRunnerInCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(1169, Warning, "Accessing the remote runner is not implemented, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogWarningRemoteRunnerUnavailable(this ILogger Logger, String TraceIdentifier);


        [LoggerMessage(2000, Information, "ActiveSession middleware is registered for addition to a middleware pipeline.")]
        public static partial void LogInformationActiveSessionMiddlewareRegistered(this ILogger Logger);

        [LoggerMessage(2001, Information, "ActiveSession middleware is added to the middleware pipeline.")]
        public static partial void LogInformationActiveSessionMiddlewareAdded(this ILogger Logger);


        [LoggerMessage(3000, Debug, "ActiveSession feature is activated, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugActiveSessionFeatureActivated(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(3100, Debug, "ActiveSessionStore constructor will use the follwing options: {Options}.")]
        public static partial void LogDebugActiveSessionStoreConstructorOptions(this ILogger Logger, ActiveSessionOptions Options);

        [LoggerMessage(3101, Debug, "ActiveSessionStore constructor will use the follwing options for its own cache: {Options}.")]
        public static partial void LogDebugActiveSessionStoreConstructorOwnCacheOptions(this ILogger Logger, MemoryCacheOptionsForLogging Options);

        [LoggerMessage(3102, Debug, "ActiveSessionStore constructor created its own cache.")]
        public static partial void LogDebugActiveSessionStoreConstructorOwnCaheCreated(this ILogger Logger);

        [LoggerMessage(3120, Debug, "ActiveSession key to use: \"{Key}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugActiveSessionKeyToUse(this ILogger Logger, String Key, String TraceIdentifier);

        [LoggerMessage(3121, Debug, "Found existing ActiveSession for the key: \"{Key}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugFoundExistingActiveSession(this ILogger Logger, String Key, String TraceIdentifier);

        [LoggerMessage(3122, Debug, "Creating new ActiveSession for the key: \"{Key}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugCreateNewActiveSession(this ILogger Logger, String Key, String TraceIdentifier);

        [LoggerMessage(3123, Debug, "Before Disposing the ActiveSession, SessionKey={SessionKey}")]
        public static partial void LogDebugBeforeSessionDisposing(this ILogger Logger, String SessionKey);

        [LoggerMessage(3124, Debug, "Exit ActiveSessionStore.FetchOrCreateSession due to the exception, the cache entry has been removed, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugFetchOrCreateExceptionalExit(this ILogger Logger, Exception AnException, String TraceIdentifier);

        [LoggerMessage(3130, Debug, "A new runner was created Number={Key}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugCreateNewRunner(this ILogger Logger, Int32 Key, String TraceIdentifier);

        [LoggerMessage(3131, Debug, "Exit ActiveSessionStore.CreateRunner due to the exception, the cache entry has been removed, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugCreateRunnerFailure(this ILogger Logger, Exception AnException, String TraceIdentifier);

        [LoggerMessage(3132, Debug, "The runner factory was fetched from the cache, TRequest=\"{TRequest}\", TResult=\"{TResult}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugGetRunnerFactoryFromCache(this ILogger Logger, String TRequest, String TResult, String TraceIdentifier);

        [LoggerMessage(3133, Debug, "The runner factory was created and is to be added to the cache, TRequest=\"{TRequest}\", TResult=\"{TResult}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugInstatiateNewRunnerFactory(this ILogger Logger, String TRequest, String TResult, String TraceIdentifier);

        [LoggerMessage(3150, Debug, "Extracting the local runner from cache, Number={Key}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugGetLocalRunnerFromCache(this ILogger Logger, Int32 Key, String TraceIdentifier);

        [LoggerMessage(3151, Debug, "Trying to make a proxy for the remote runner, Number={Key}, HostId={HostId}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugProcessRemoteRunner(this ILogger Logger, Int32 Key, String HostId, String TraceIdentifier);


#if TRACE
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

        [LoggerMessage(4011, Trace, "Invoking the rest of the middleware pipeline after the ActiveSession middleware, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareInvokeRest(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4012, Trace, "Control from the rest of the pipeline was returned without exceptions, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareControlReturns(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4013, Trace, "An exception in the the pipeline has been caught, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTracePipelineException(this ILogger Logger, Exception AnException, String TraceIdentifier);

        [LoggerMessage(4014, Trace, "ActiveSession middleware exited, TraceIdentifier=\"{TraceIdentifier}\".")]
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

        [LoggerMessage(4120, Trace, "Eneter ActiveSessionStore.FetchOrCreateSession, TraceIdentifier=\"{TraceIdentifier}\". ")]
        public static partial void LogTraceFetchOrCreate(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4121, Trace, "Exit ActiveSessionStore.FetchOrCreateSession, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceFetchOrCreateExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4122, Trace, "Enter ActiveSession eviction callback, SessionKey={SessionKey}")]
        public static partial void LogTraceSessionEvictionCallback(this ILogger Logger, String SessionKey);

        [LoggerMessage(4123, Trace, "Evict all runners of the evicted session, SessionKey={SessionKey}")]
        public static partial void LogTraceEvictRunners(this ILogger Logger, String SessionKey);

        [LoggerMessage(4124, Trace, "Exit ActiveSession EvictionCallback, SessionKey={SessionKey}")]
        public static partial void LogTraceSessionEvictionCallbackExit(this ILogger Logger, String SessionKey);

        [LoggerMessage(4130, Trace, "Enter ActiveSessionStore.CreateRunner, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceCreateRunner(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4131, Trace, "New runner to be created, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceNewRunnerInfoRunner(this ILogger Logger, String SessionKey, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(4132, Trace, "Registering the new runner completion callback, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceSettingRunnerCompletionCallback(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4133, Trace, "Exiting ActiveSessionStore.CreateRunner, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceCreateRunnerExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4134, Trace, "Enter ActiveSessionStore.GetRunnerFactory, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerFactory(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4135, Trace, "Storing the factory in the cache, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceStoreNewRunnerFactoryInCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4136, Trace, "Exiting ActiveSessionStore.GetRunnerFactory, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerFactoryExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4140, Trace, "The ActiveSessionStore.RunnerCompletionCallback entered, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunnerCompletionCallback(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4141, Trace, "Evicting the completed runner, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunnerCompletionRemoveAborted(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4142, Trace, "The ActiveSessionStore.RunnerCompletionCallback exited, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunnerCompletionCallbackExit(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4143, Trace, "The ActiveSessionStore.RunnerEvictionCallback entered, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunnerEvictionCallback(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4144, Trace, "Disposing the runner, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceDisposeRunner(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4145, Trace, "The ActiveSessionStore.RunnerEvictionCallback exited, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunnerEvictionCallbackExit(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4150, Trace, "Entering ActiveSessionStore.GetRunner, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunner(this ILogger Logger, String SessionKey, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(4151, Trace, "No value for the runner in the session the was found, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceNoRunnerInSession(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4152, Trace, "Exiting ActiveSessionStore.GetRunner, RunnerFound={RunnerFound}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerExit(this ILogger Logger, Boolean RunnerFound, String TraceIdentifier);

        [LoggerMessage(4153, Trace, "Enter ActiveSessionStore.GetRunnerAsync, awaiting until the session is loaded, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerAsync(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4154, Trace, "The session is loaded and the runner info is known now: SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerAsyncInfoRunner(this ILogger Logger, String SessionKey, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(4155, Trace, "Awaiting until the proxy is created, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceAwaitForProxyCreation(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4156, Trace, "Exiting ActiveSessionStore.GetRunnerAsync, RunnerFound={RunnerFound}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerAsyncExit(this ILogger Logger, Boolean RunnerFound, String TraceIdentifier);

        [LoggerMessage(4160, Trace, "Enter ActiveSessionStore.RegisterRunnerInSession, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceRegisterRunnerInSession(this ILogger Logger, String SessionKey, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(4161, Trace, "Exit ActiveSessionStore.RegisterRunnerInSession, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceRegisterRunnerInSessionExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4162, Trace, "Enter ActiveSessionStore.UnregisterRunnerInSession, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerInSession(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4163, Trace, "Exit ActiveSessionStore.UnregisterRunnerInSession, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerInSessionExit(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4164, Trace, "Enter ActiveSessionStore.ExtractRunnerFromCache, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceExtractRunnerFromCache(this ILogger Logger, String SessionKey, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(4165, Trace, "The local runner is found in the cache, returning it, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceReturnRunnerFromCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4166, Trace, "The local runner is not found in the cache, while being registered in the session, unregistering it, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceNoExpectedRunnerInCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4167, Trace, "Exit ActiveSessionStore.ExtractRunnerFromCache, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceExtractRunnerFromCacheExit(this ILogger Logger, String TraceIdentifier);

        /*
         */
#endif

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
