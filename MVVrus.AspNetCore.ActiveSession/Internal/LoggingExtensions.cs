using Microsoft.Extensions.Caching.Memory;
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

        [LoggerMessage(5, Error, "Cannot acquire a new runner number, SessionId={SessionId}, TraceIdentifier=\"{TraceIdentifier}\". ")]
        public static partial void LogErrorCannotAllocateRunnerNumber(this ILogger Logger, String SessionId, String TraceIdentifier);


        [LoggerMessage(1000, Warning, "Cannot obtain an ActiveSession store service from the application service container. The ActiveSession middleware cannot be registered. Was the IServiceCollection.AddActiveServices extension method called at least once?")]
        public static partial void LogWarningAbsentFactoryInplementations(this ILogger Logger, Exception AnException);

        [LoggerMessage(1100, Warning, "ActiveSessionStore constructor: cannot create our own cache, fall back to the shared cache.")]
        public static partial void LogWarningActiveSessionStoreCannotCreateOwnCache(this ILogger Logger, Exception AnException);

        [LoggerMessage(1160, Warning, "The runner is found in the local cache but cannot be returned: the runner type is incompatible, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogWarningNoExpectedRunnerInCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(1169, Warning, "Accessing the remote runner is not implemented, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogWarningRemoteRunnerUnavailable(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(1300, Warning, "The exception occered while establishing ActiveSessionFeature.ActiveSession property, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogWarningActiveSessionLoad(this ILogger Logger, Exception exception, String TraceIdentifier);

        [LoggerMessage(1500, Warning, "Attempt to register the runner after it's Active Session cleanup initiation, an exception to be throwed, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber} ")]
        public static partial void LogWarningRegisterRunnerAfterCleanupInit(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(2000, Information, "ActiveSession middleware is registered for addition to a middleware pipeline.")]
        public static partial void LogInformationActiveSessionMiddlewareRegistered(this ILogger Logger);

        [LoggerMessage(2001, Information, "ActiveSession middleware is added to the middleware pipeline.")]
        public static partial void LogInformationActiveSessionMiddlewareAdded(this ILogger Logger);


        [LoggerMessage(3000, Debug, "ActiveSession feature is activated, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugActiveSessionFeatureActivated(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(3001, Debug, "Original RequestServices are substituted by the ActiveSession.SessionServices , TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugRequestServicesChangedToSessionServices(this ILogger Logger, String TraceIdentifier);

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

        [LoggerMessage(3130, Debug, "A new runner was created RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugCreateNewRunner(this ILogger Logger, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(3131, Debug, "Exit ActiveSessionStore.CreateRunner due to the exception, the cache entry has been removed, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugCreateRunnerFailure(this ILogger Logger, Exception AnException, String TraceIdentifier);

        [LoggerMessage(3132, Debug, "The runner factory was fetched from the cache, TRequest=\"{TRequest}\", TResult=\"{TResult}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugGetRunnerFactoryFromCache(this ILogger Logger, String TRequest, String TResult, String TraceIdentifier);

        [LoggerMessage(3133, Debug, "The runner factory was created and is to be added to the cache, TRequest=\"{TRequest}\", TResult=\"{TResult}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugInstatiateNewRunnerFactory(this ILogger Logger, String TRequest, String TResult, String TraceIdentifier);

        [LoggerMessage(3150, Debug, "Extracting the local runner from cache, RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugGetLocalRunnerFromCache(this ILogger Logger, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(3151, Debug, "Trying to make a proxy for the remote runner, RunnerNumber={RunnerNumber}, HostId={HostId}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugProcessRemoteRunner(this ILogger Logger, Int32 RunnerNumber, String HostId, String TraceIdentifier);

        [LoggerMessage(3510, Debug, "The runner to be unregistered is not registered, SessionId=\"{SessionId}\" RunnerNumber={RunnerNumber}.")]
        public static partial void LogDebugUnregisterRunnerNotRegistered(this ILogger Logger, String SessionId, Int32 RunnerNumber);


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

        [LoggerMessage(4011, Trace, "Awaiting while loading ActiveSession for SessionServices, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceWaitingForActiveSessionLoading(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4012, Trace, "Completed attempt to substituste RequestServices by SessionServices, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceCompleteRequestServicesSubstitutionAttempt(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4013, Trace, "Invoking the rest of the middleware pipeline after the ActiveSession middleware, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareInvokeRest(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4014, Trace, "Control from the rest of the pipeline was returned without exceptions, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareControlReturns(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4015, Trace, "An exception in the the pipeline has been caught, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTracePipelineException(this ILogger Logger, Exception AnException, String TraceIdentifier);

        [LoggerMessage(4016, Trace, "ActiveSession middleware exited, TraceIdentifier=\"{TraceIdentifier}\".")]
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

        [LoggerMessage(4121, Trace, "ActiveSessionStore.FetchOrCreateSession: acquiring session creation lock, TraceIdentifier=\"{TraceIdentifier}\". ")]
        public static partial void LogTraceAcquiringSessionCreationLock(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4122, Trace, "ActiveSessionStore.FetchOrCreateSession: acquired session creation lock, TraceIdentifier=\"{TraceIdentifier}\". ")]
        public static partial void LogTraceAcquiredSessionCreationLock(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4123, Trace, "ActiveSessionStore.FetchOrCreateSession: released session creation lock, TraceIdentifier=\"{TraceIdentifier}\". ")]
        public static partial void LogTraceReleasedSessionCreationLock(this ILogger Logger, String TraceIdentifier); 

        [LoggerMessage(4124, Trace, "Exit ActiveSessionStore.FetchOrCreateSession, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceFetchOrCreateExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4125, Trace, "Enter ActiveSession eviction callback, SessionKey={SessionKey}")]
        public static partial void LogTraceSessionEvictionCallback(this ILogger Logger, String SessionKey);

        [LoggerMessage(4126, Trace, "Evict all runners of the evicted session, SessionKey={SessionKey}")]
        public static partial void LogTraceEvictRunners(this ILogger Logger, String SessionKey);

        [LoggerMessage(4127, Trace, "Cleanup of all runners for the ActiveSession is initiated, SessionKey={SessionKey}")]
        public static partial void LogTraceSessionCleanupRunnersInitiated(this ILogger Logger, String SessionKey);

        [LoggerMessage(4128, Trace, "The ActiveSession service container scope to be disposed, SessionKey={SessionKey}")]
        public static partial void LogTraceSessionScopeToBeDisposed(this ILogger Logger, String SessionKey);

        [LoggerMessage(4129, Trace, "Exit ActiveSession EvictionCallback, SessionKey={SessionKey}")]
        public static partial void LogTraceSessionEvictionCallbackExit(this ILogger Logger, String SessionKey);

        [LoggerMessage(4130, Trace, "Enter ActiveSessionStore.CreateRunner, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceCreateRunner(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4131, Trace, "New runner to be created, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceNewRunnerInfoRunner(this ILogger Logger, String SessionKey, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(4132, Trace, "Waive to use the runner number previousely acquired because of an exception, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceWaiveRunnerNumber(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4133, Trace, "Acquiring the runner creation lock for session \"{SessionKey}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceFallbackToStoreGlobalLock(this ILogger Logger, String SessionKey, String TraceIdentifier);

        [LoggerMessage(4134, Trace, "Acquiring the runner creation lock for session \"{SessionKey}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceAcquiringRunnerCreationLock(this ILogger Logger, String SessionKey, String TraceIdentifier);

        [LoggerMessage(4135, Trace, "Acquired the runner creation lock for session \"{SessionKey}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceAcquiredRunnerCreationLock(this ILogger Logger, String SessionKey, String TraceIdentifier);

        [LoggerMessage(4136, Trace, "Released the runner creation lock for session \"{SessionKey}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceReleasedRunnerCreationLock(this ILogger Logger, String SessionKey, String TraceIdentifier);

        [LoggerMessage(4137, Trace, "Exiting ActiveSessionStore.CreateRunner, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceCreateRunnerExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4138, Trace, "Enter ActiveSessionStore.GetRunnerFactory, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerFactory(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4139, Trace, "Storing the factory in the cache, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceStoreNewRunnerFactoryInCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4140, Trace, "Exiting ActiveSessionStore.GetRunnerFactory, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerFactoryExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4141, Trace, "The ActiveSessionStore.RunnerEvictionCallback entered, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunnerEvictionCallback(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4142, Trace, "Disposing the runner, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceDisposeRunner(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4143, Trace, "The ActiveSessionStore.RunnerEvictionCallback exited, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunnerEvictionCallbackExit(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4144, Trace, "Strting task observing completion of all runners for the ActiveSession, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceActiveSessionCompleteDispose(this ILogger Logger, String SessionId);

        [LoggerMessage(4145, Trace, "Observing timely completion of all runners for the session done, SessionId=\"{SessionId}\", WithinTimeout={WaitSucceeded}.")]
        public static partial void LogTraceActiveSessionEndWaitingForRunnersCompletion(this ILogger Logger, String SessionId, Boolean WaitSucceeded);

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

        [LoggerMessage(4163, Trace, "Perorming the runner unregistration in the session, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTracePerformUnregisteration(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4164, Trace, "Exit ActiveSessionStore.UnregisterRunnerInSession, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerInSessionExit(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(4165, Trace, "Enter ActiveSessionStore.ExtractRunnerFromCache, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceExtractRunnerFromCache(this ILogger Logger, String SessionKey, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(4166, Trace, "The local runner is found in the cache, returning it, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceReturnRunnerFromCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4167, Trace, "The local runner is not found in the cache, while being registered in the session (possibly evicted), unregister it, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceNoExpectedRunnerInCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4168, Trace, "Exit ActiveSessionStore.ExtractRunnerFromCache, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceExtractRunnerFromCacheExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4200, Trace, "Enter ActiveSession Constructor, SessionId=\"{SessionId}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionConstructor(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(4201, Trace, "Exit ActiveSession Constructor, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionConstructorExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4210, Trace, "Enter ActiveSession.CreateRunner, SessionId=\"{SessionId}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionCreateRunner(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(4211, Trace, "Exit ActiveSession.CreateRunner, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceCreateActiveSessionCreateRunnerExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4220, Trace, "Enter ActiveSession.GetRunner, SessionId=\"{SessionId}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionGetRunner(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(4221, Trace, "Exit ActiveSession.GetRunner, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionGetRunnerExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4230, Trace, "Enter ActiveSession.GetRunnerAsync, SessionId=\"{SessionId}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionGetRunnerAsync(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(4231, Trace, "Exit ActiveSession.GetRunnerAsync, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionGetRunnerAsyncExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4240, Trace, "Disposing ActiveSession, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceActiveSessionDispose(this ILogger Logger, String SessionId);

        [LoggerMessage(4300, Trace, "Enter ActiveSessionFeature constructor, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureConstructor(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4301, Trace, "Exit ActiveSessionFeature constructor, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureConstructorExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4310, Trace, "Enter ActiveSessionFeature.CommitAsync, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureCommitAsync(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4311, Trace, "Await for ActiveSession committing , TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureCommitAsyncActiveSessionWait(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4312, Trace, "Exit ActiveSessionFeature.CommitAsync, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureCommitAsyncExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4320, Trace, "Enter ActiveSessionFeature.Clear, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureClear(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4321, Trace, "Clearing reference to the ActiveSession, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeaturePerformClear(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4322, Trace, "Exit ActiveSessionFeature.Clear, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureClearExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4330, Trace, "Enter ActiveSessionFeature.LoadAsync, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadAsync(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4331, Trace, "Perform ActiveSessionFeature.ActiveSession property initialization, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncLoading(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4332, Trace, "Await for Session loading, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncSessionWait(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4333, Trace, "The Session has been loaded, proceed, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncSessionWaitEnded(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4334, Trace, "Create or fetch from cache the ActiveSession object, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncGetActiveSession(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4335, Trace, "Exit ActiveSessionFeature.LoadAsync, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4340, Trace, "Enter ActiveSessionFeature.Load, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoad(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4341, Trace, "Load Session synchronously and create or fetch from cache the ActiveSession object, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadGetActiveSession(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4342, Trace, "Exit ActiveSessionFeature.Load, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4350, Trace, "Enter ActiveSessionFeature.SetSession, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureSetSession(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4351, Trace, "Exit ActiveSessionFeature.SetSessiond, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureSetSessionExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4400, Trace, "Creating delegate runner factory to implement IActiveSessionRunnerFactory<{TRequest}, {TResult}>.")]
        public static partial void LogTraceConstructDelegateFactory(this ILogger Logger, String TRequest, String TResult);

        [LoggerMessage(4401, Trace, "Invoke delegate runner factory implementing IActiveSessionRunnerFactory<{TRequest}, {TResult}>.")]
        public static partial void LogTraceInvokingDelegateFactory(this ILogger Logger, String TRequest, String TResult);

        [LoggerMessage(4410, Trace, "Creating type-based runner factory to implement IActiveSessionRunnerFactory<{TRequest}, {TResult}> via {ImplementingClassName}.")]
        public static partial void LogTraceConstructTypeFactory(this ILogger Logger, String TRequest, String TResult, String ImplementingClassName);

        [LoggerMessage(4411, Trace, "Invoke type-based runner factory implementing IActiveSessionRunnerFactory<{TRequest}, {TResult}>.")]
        public static partial void LogTraceInvokingTypeFactory(this ILogger Logger, String TRequest, String TResult);

        [LoggerMessage(4500, Trace, "DefaultRunnerManager.RegisterRunner entered, acquiring runners lock SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRegisterRunnerEnter(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4501, Trace, "DefaultRunnerManager.RegisterRunner: runners lock acquired, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRegisterRunnerLockAcquired(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4502, Trace, "DefaultRunnerManager.RegisterRunner exited, runners lock released, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRegisterRunnerExit(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4510, Trace, "DefaultRunnerManager.UnregisterRunner entered, acquiring runners lock, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunner(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4511, Trace, "DefaultRunnerManager.UnregisterRunner runners lock acquired, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerLockAcquired(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4512, Trace, "DefaultRunnerManager.UnregisterRunner, performing the runner unregistartion and cleanup SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerRemove(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4513, Trace, "DefaultRunnerManager, preparing task for disposal of the runner, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTracePrepareDisposeTask(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4514, Trace, "DefaultRunnerManager, run a task returned by DisposeAsync(), SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunAsyncDisposeTask(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4515, Trace, "DefaultRunnerManager, run Dispose() in a separate task, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunDisposeTask(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4516, Trace, "DefaultRunnerManager, no disposal needed, run no task, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunNoDisposeTask(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4517, Trace, "DefaultRunnerManager.UnregisterRunner exited, runners lock released, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerExit(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4520, Trace, "Enter DefaultRunnerManager.GetNewRunnerNumber, SessionId=\"{SessionId}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetNewRunnerNumber(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(4521, Trace, "Exit DefaultRunnerManager.GetNewRunnerNumber returning the number, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetNewRunnerNumberExit(this ILogger Logger, String SessionId, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(4530, Trace, "DefaultRunnerManager: Return back unused runner number, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceReturnRunnerNumber(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4540, Trace, "DefaultRunnerManager.GetRunnerInfo entered, acquiring runners lock, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceGetRunnerInfo(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4541, Trace, "DefaultRunnerManager.GetRunnerInfo runners lock acquired, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceGetRunnerInfoLockAcquired(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4542, Trace, "DefaultRunnerManager.GetRunnerInfo exited, runners lock released, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}, RunnerInfo found: {Found}.")]
        public static partial void LogTraceGetRunnerInfoExit(this ILogger Logger, String SessionId, Int32 RunnerNumber, Boolean Found);

        [LoggerMessage(4550, Trace, "DefaultRunnerManager.FinishDisposalTask entered, acquiring runners lock, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceFinishDisposalTask(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4551, Trace, "DefaultRunnerManager.FinishDisposalTask runners lock acquired, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceFinishDisposalTaskLockAcquired(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4552, Trace, "DefaultRunnerManager.FinishDisposalTask exited, runners lock released, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}")]
        public static partial void LogTraceFinishDisposalTaskExit(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4560, Trace, "DefaultRunnerManager.AbortAll entered, acquiring runners lock, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceAbortAll(this ILogger Logger, String SessionId);

        [LoggerMessage(4561, Trace, "DefaultRunnerManager.AbortAll entered, runners lock acquired, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceAbortAllLockAcqired(this ILogger Logger, String SessionId);

        [LoggerMessage(4562, Trace, "DefaultRunnerManager.AbortAll, aborting a runner, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}")]
        public static partial void LogTraceAbortAllAbortRunner(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(4563, Trace, "DefaultRunnerManager.AbortAll exited, runners lock released, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceAbortAllExit(this ILogger Logger, String SessionId);

        [LoggerMessage(4570, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync started, runners lock acquired, SessionId=\"{SessionId}\".")]
        public static partial void LogTracePerformRunnersCleanup(this ILogger Logger, String SessionId);

        [LoggerMessage(4571, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync, runners lock released, awaiting CleanupRunners , SessionId=\"{SessionId}\".")]
        public static partial void LogTracePerformRunnersCleanupAwaiting(this ILogger Logger, String SessionId);

        [LoggerMessage(4572, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync disposing this DefaultRunnerManger instance, SessionId=\"{SessionId}\".")]
        public static partial void LogTracePerformRunnersCleanupDisposing(this ILogger Logger, String SessionId);

        [LoggerMessage(4573, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync finished, SessionId=\"{SessionId}\".")]
        public static partial void LogTracePerformRunnersCleanupComplete(this ILogger Logger, String SessionId);

        [LoggerMessage(4574, Trace, "DefaultRunnerManager.CleanupRunners entered, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceCleanupRunners(this ILogger Logger, String SessionId);

        [LoggerMessage(4575, Trace, "DefaultRunnerManager.CleanupRunners waiting for all runners to unregister, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceCleanupRunnersWaitForUnregistration(this ILogger Logger, String SessionId);

        [LoggerMessage(4576, Trace, "DefaultRunnerManager.CleanupRunners acquiring runners lock, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceCleanupRunnersAcquiringLock(this ILogger Logger, String SessionId);

        [LoggerMessage(4577, Trace, "DefaultRunnerManager.CleanupRunners runners lock acquired, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceCleanupRunnersLockAcquired(this ILogger Logger, String SessionId);

        [LoggerMessage(4578, Trace, "DefaultRunnerManager.CleanupRunners runners lock released, returning a task awaiting disposing all runners, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceCleanupRunnersReturnContinuation(this ILogger Logger, String SessionId);


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
