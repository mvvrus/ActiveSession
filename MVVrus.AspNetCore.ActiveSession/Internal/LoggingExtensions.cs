using Microsoft.Extensions.Caching.Memory;
using System;
using static Microsoft.Extensions.Logging.LogLevel;
using static MVVrus.AspNetCore.ActiveSession.Internal.LogIds;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal static partial class LoggingExtensions
    {
        [LoggerMessage(E_MIDDLEWARE, Error, "The exception occured while creating the ActiveSession middleware.")]
        public static partial void LogErrorMiddlewareCannotBeCreated(this ILogger Logger, Exception AnException);

        [LoggerMessage(E_NOCACHE, Error, "Shared cache to be used is not available")]
        public static partial void LogErrorNoSharedCacheException(this ILogger Logger);

        [LoggerMessage(E_FAILCREATERUNNER, Error, "The factory failed to create a runner and returned null, TraceIdentifier=\"{TraceIdentifier}\". ")]
        public static partial void LogErrorCreateRunnerFailure(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(E_REMOTERUNNER, Error, "Using remote runners is not allowed by configuration setting ThrowOnRemoteRunner, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogErrorRemoteRunnerUnavailable(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(E_NORUNNERNUMBER, Error, "Cannot acquire a new runner number, SessionId={SessionId}, TraceIdentifier=\"{TraceIdentifier}\". ")]
        public static partial void LogErrorCannotAllocateRunnerNumber(this ILogger Logger, String SessionId, String TraceIdentifier);


        [LoggerMessage(W_NOSTORE, Warning, "Cannot obtain an ActiveSession store service from the application service container. The ActiveSession middleware cannot be registered. Was the IServiceCollection.AddActiveServices extension method called at least once?")]
        public static partial void LogWarningAbsentFactoryInplementations(this ILogger Logger, Exception AnException);

        [LoggerMessage(W_NOOWNCACHE, Warning, "ActiveSessionStore constructor: cannot create our own cache, fall back to the shared cache.")]
        public static partial void LogWarningActiveSessionStoreCannotCreateOwnCache(this ILogger Logger, Exception AnException);

        [LoggerMessage(W_INCOMPATRUNNERTYPE, Warning, "The runner is found in the local cache but cannot be returned: the runner type is incompatible, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogWarningNoExpectedRunnerInCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(W_REMOTENOTIMPLMENTED, Warning, "Accessing the remote runner is not implemented, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogWarningRemoteRunnerUnavailable(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(W_ACTIVESESSIONFAIL, Warning, "The exception occered while establishing ActiveSessionFeature.ActiveSession property, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogWarningActiveSessionLoad(this ILogger Logger, Exception exception, String TraceIdentifier);

        [LoggerMessage(W_REGISTERDURINGCLEANUP, Warning, "Attempt to register the runner after it's Active Session cleanup initiation, an exception to be throwed, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber} ")]
        public static partial void LogWarningRegisterRunnerAfterCleanupInit(this ILogger Logger, String SessionId, Int32 RunnerNumber);


        [LoggerMessage(W_MIDDLEWAREREGISTERED, Information, "ActiveSession middleware is registered for addition to a middleware pipeline.")]
        public static partial void LogInformationActiveSessionMiddlewareRegistered(this ILogger Logger);

        [LoggerMessage(W_MIDDLEWAREADDED, Information, "ActiveSession middleware is added to the middleware pipeline.")]
        public static partial void LogInformationActiveSessionMiddlewareAdded(this ILogger Logger);


        [LoggerMessage(D_FEATUREACTIVATED, Debug, "ActiveSession feature is activated, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugActiveSessionFeatureActivated(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(D_SERVICESSUBSTITUTED, Debug, "Original RequestServices are substituted by the ActiveSession.SessionServices , TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugRequestServicesChangedToSessionServices(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(D_STORECONSTRUCTOROPTIONS, Debug, "ActiveSessionStore constructor will use the follwing options: {Options}.")]
        public static partial void LogDebugActiveSessionStoreConstructorOptions(this ILogger Logger, ActiveSessionOptions Options);

        [LoggerMessage(D_STORECACHEOPTIONS, Debug, "ActiveSessionStore constructor will use the follwing options for its own cache: {Options}.")]
        public static partial void LogDebugActiveSessionStoreConstructorOwnCacheOptions(this ILogger Logger, MemoryCacheOptionsForLogging Options);

        [LoggerMessage(D_STORECONSTRUCTORCACHECREATED, Debug, "ActiveSessionStore constructor created its own cache.")]
        public static partial void LogDebugActiveSessionStoreConstructorOwnCaheCreated(this ILogger Logger);

        [LoggerMessage(D_STORESESSIONKEY, Debug, "ActiveSession key to use: \"{Key}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugActiveSessionKeyToUse(this ILogger Logger, String Key, String TraceIdentifier);

        [LoggerMessage(D_STORESESSIONFOUND, Debug, "Found existing ActiveSession for the key: \"{Key}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugFoundExistingActiveSession(this ILogger Logger, String Key, String TraceIdentifier);

        [LoggerMessage(D_STORESESSIONTOCREATE, Debug, "Creating new ActiveSession for the key: \"{Key}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugCreateNewActiveSession(this ILogger Logger, String Key, String TraceIdentifier);

        [LoggerMessage(D_STORESESSIONCREATEDDISPOSE, Debug, "Before Disposing the ActiveSession, SessionKey={SessionKey}")]
        public static partial void LogDebugBeforeSessionDisposing(this ILogger Logger, String SessionKey);

        [LoggerMessage(D_STORESESSIONEXIT, Debug, "Exit ActiveSessionStore.FetchOrCreateSession due to the exception, the cache entry has been removed, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugFetchOrCreateExceptionalExit(this ILogger Logger, Exception AnException, String TraceIdentifier);

        [LoggerMessage(D_STORERUNNERCREATED, Debug, "A new runner was created RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugCreateNewRunner(this ILogger Logger, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(D_STORERUNNERCREATIONFAIL, Debug, "Exit ActiveSessionStore.CreateRunner due to the exception, the cache entry has been removed, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugCreateRunnerFailure(this ILogger Logger, Exception AnException, String TraceIdentifier);

        [LoggerMessage(D_STORERUNNERFACTORYFOUND, Debug, "The runner factory was fetched from the cache, TRequest=\"{TRequest}\", TResult=\"{TResult}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugGetRunnerFactoryFromCache(this ILogger Logger, String TRequest, String TResult, String TraceIdentifier);

        [LoggerMessage(D_STORERUNNERFACTORYNEW, Debug, "The runner factory was created and is to be added to the cache, TRequest=\"{TRequest}\", TResult=\"{TResult}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugInstatiateNewRunnerFactory(this ILogger Logger, String TRequest, String TResult, String TraceIdentifier);

        [LoggerMessage(D_STORERUNNERFOUND, Debug, "Extracting the local runner from cache, RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugGetLocalRunnerFromCache(this ILogger Logger, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(D_STORERUNNERPROXY, Debug, "Trying to make a proxy for the remote runner, RunnerNumber={RunnerNumber}, HostId={HostId}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogDebugProcessRemoteRunner(this ILogger Logger, Int32 RunnerNumber, String HostId, String TraceIdentifier);

        [LoggerMessage(D_MANAGERRUNNERNOTREGISTERED, Debug, "The runner to be unregistered is not registered, SessionId=\"{SessionId}\" RunnerNumber={RunnerNumber}.")]
        public static partial void LogDebugUnregisterRunnerNotRegistered(this ILogger Logger, String SessionId, Int32 RunnerNumber);


#if TRACE
        [LoggerMessage(T_BUILDUSE, Trace, "Enetering ActiveSessionBuilderExtensions.UseActiveSessions.")]
        public static partial void LogTraceUseActiveSessions(this ILogger Logger);

        [LoggerMessage(T_BUILDUSEEXIT, Trace, "Exiting ActiveSessionBuilderExtensions.UseActiveSessions.")]

        public static partial void LogTraceUseActiveSessionsExit(this ILogger Logger);
        [LoggerMessage(T_BUILDCONS, Trace, "Enetering ActiveSessionMiddleware constructor.")]
        public static partial void LogTraceConstructActiveSessionMiddleware(this ILogger Logger);

        [LoggerMessage(T_BUILDCONSEXIT, Trace, "Exiting ActiveSessionMiddleware constructor.")]
        public static partial void LogTraceConstructActiveSessionMiddlewareExit(this ILogger Logger);

        [LoggerMessage(T_MIDDLEWARE, Trace, "Entering ActiveSessionMiddleware, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceInvokeActiveSessionMiddleware(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWARELOADAWAIT, Trace, "Awaiting while loading ActiveSession for SessionServices, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceWaitingForActiveSessionLoading(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWARESERVICESSUBSTITUTED, Trace, "Completed attempt to substituste RequestServices by SessionServices, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceCompleteRequestServicesSubstitutionAttempt(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWARENEXTINVOKE, Trace, "Invoking the rest of the middleware pipeline after the ActiveSession middleware, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareInvokeRest(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWARENEXTRETURN, Trace, "Control from the rest of the pipeline was returned without exceptions, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareControlReturns(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWARENEXTEXCEPTION, Trace, "An exception in the the pipeline has been caught, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTracePipelineException(this ILogger Logger, Exception AnException, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWAREEXIT, Trace, "ActiveSession middleware exited, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORECONS, Trace, "The ActiveSessionStore constructor entered.")]
        public static partial void LogTraceActiveSessionStoreConstructor(this ILogger Logger);

        [LoggerMessage(T_STORECONSOWNCACHE, Trace, "ActiveSessionStore constructor before create its own cache")]
        public static partial void LogTraceLogTraceActiveSessionStoreConstructorCreatingOwnCache(this ILogger Logger);

        [LoggerMessage(T_STORECONSSHAREDCACHE, Trace, "ActiveSessionStore constructor use shared cache")]
        public static partial void LogTraceActiveSessionStoreConstructorUseSharedCache(this ILogger Logger);

        [LoggerMessage(T_STORECONSEXIT, Trace, "The ActiveSessionStore constructor exited.")]
        public static partial void LogTraceActiveSessionStoreConstructorExit(this ILogger Logger);

        [LoggerMessage(T_STOREDISPOSING, Trace, "Disposing ActiveSessionStore object")]
        public static partial void LogTraceActiveSessionStoreDisposing(this ILogger Logger);

        [LoggerMessage(T_STORESESSION, Trace, "Eneter ActiveSessionStore.FetchOrCreateSession, TraceIdentifier=\"{TraceIdentifier}\". ")]
        public static partial void LogTraceFetchOrCreate(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORESESSIONACQUIRING, Trace, "ActiveSessionStore.FetchOrCreateSession: acquiring session creation lock, TraceIdentifier=\"{TraceIdentifier}\". ")]
        public static partial void LogTraceAcquiringSessionCreationLock(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORESESSIONACQUIRED, Trace, "ActiveSessionStore.FetchOrCreateSession: acquired session creation lock, TraceIdentifier=\"{TraceIdentifier}\". ")]
        public static partial void LogTraceAcquiredSessionCreationLock(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORESESSIONRELEASED, Trace, "ActiveSessionStore.FetchOrCreateSession: released session creation lock, TraceIdentifier=\"{TraceIdentifier}\". ")]
        public static partial void LogTraceReleasedSessionCreationLock(this ILogger Logger, String TraceIdentifier); 

        [LoggerMessage(T_STORESESSIONEXIT, Trace, "Exit ActiveSessionStore.FetchOrCreateSession, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceFetchOrCreateExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORESESSIONCALLBACK, Trace, "Enter ActiveSession eviction callback, SessionKey={SessionKey}")]
        public static partial void LogTraceSessionEvictionCallback(this ILogger Logger, String SessionKey);

        [LoggerMessage(T_STORESESSIONCALLBACKABORTALL, Trace, "Abort all runners of the evicted session, SessionKey={SessionKey}")]
        public static partial void LogTraceAbortRunners(this ILogger Logger, String SessionKey);

        [LoggerMessage(T_STORESESSIONCALLBACKCLEANUPSTART, Trace, "Cleanup of all runners for the ActiveSession is initiated, SessionKey={SessionKey}")]
        public static partial void LogTraceSessionCleanupRunnersInitiated(this ILogger Logger, String SessionKey);

        [LoggerMessage(T_STORESESSIONCALLBACKDISPOSINGSCOPE, Trace, "The ActiveSession service container scope to be disposed, SessionKey={SessionKey}")]
        public static partial void LogTraceSessionScopeToBeDisposed(this ILogger Logger, String SessionKey);

        [LoggerMessage(T_STORESESSIONCALLBACKEXIT, Trace, "Exit ActiveSession EvictionCallback, SessionKey={SessionKey}")]
        public static partial void LogTraceSessionEvictionCallbackExit(this ILogger Logger, String SessionKey);

        [LoggerMessage(T_STORENEWRUNNER, Trace, "Enter ActiveSessionStore.CreateRunner, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceCreateRunner(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNERTOCREATE, Trace, "New runner to be created, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceNewRunnerInfoRunner(this ILogger Logger, String SessionKey, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNERWAIVENUMBER, Trace, "Waive to use the runner number previousely acquired because of an exception, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceWaiveRunnerNumber(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNERLOCKFALLBACK, Trace, "Acquiring the runner creation lock for session \"{SessionKey}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceFallbackToStoreGlobalLock(this ILogger Logger, String SessionKey, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNERACQUIRING, Trace, "Acquiring the runner creation lock for session \"{SessionKey}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceAcquiringRunnerCreationLock(this ILogger Logger, String SessionKey, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNERACQUIRED, Trace, "Acquired the runner creation lock for session \"{SessionKey}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceAcquiredRunnerCreationLock(this ILogger Logger, String SessionKey, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNERRELEASED, Trace, "Released the runner creation lock for session \"{SessionKey}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceReleasedRunnerCreationLock(this ILogger Logger, String SessionKey, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNEREXIT, Trace, "Exiting ActiveSessionStore.CreateRunner, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceCreateRunnerExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERFACTORY, Trace, "Enter ActiveSessionStore.GetRunnerFactory, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerFactory(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERFACTORYCACHE, Trace, "Storing the factory in the cache, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceStoreNewRunnerFactoryInCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERFACTORYEXIT, Trace, "Exiting ActiveSessionStore.GetRunnerFactory, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerFactoryExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERCALLBACK, Trace, "The ActiveSessionStore.RunnerEvictionCallback entered, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunnerEvictionCallback(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(T_STORERUNNERCALLBACKEXIT, Trace, "The ActiveSessionStore.RunnerEvictionCallback exited, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunnerEvictionCallbackExit(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(T_STORERUNNERCLEANUP, Trace, "Strting task observing completion of all runners for the ActiveSession, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceActiveSessionCompleteDispose(this ILogger Logger, String SessionId);

        [LoggerMessage(T_STORERUNNERCLEANUPRESULT, Trace, "Observing timely completion of all runners for the session done, SessionId=\"{SessionId}\", WithinTimeout={WaitSucceeded}.")]
        public static partial void LogTraceActiveSessionEndWaitingForRunnersCompletion(this ILogger Logger, String SessionId, Boolean WaitSucceeded);

        [LoggerMessage(T_STOREGETRUNNER, Trace, "Entering ActiveSessionStore.GetRunner, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunner(this ILogger Logger, String SessionKey, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_STOREGETRUNNERCLEANUPVARS, Trace, "No value for the runner in the session the was found, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceNoRunnerInSession(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STOREGETRUNNEREXIT, Trace, "Exiting ActiveSessionStore.GetRunner, RunnerFound={RunnerFound}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerExit(this ILogger Logger, Boolean RunnerFound, String TraceIdentifier);

        [LoggerMessage(T_STOREGETRUNNERASYNC, Trace, "Enter ActiveSessionStore.GetRunnerAsync, awaiting until the session is loaded, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerAsync(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STOREGETRUNNERASYNCLOADED, Trace, "The session is loaded and the runner info is known now: SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerAsyncInfoRunner(this ILogger Logger, String SessionKey, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_STOREGETRUNNERASYNCWAITPROXY, Trace, "Awaiting until the proxy is created, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceAwaitForProxyCreation(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STOREGETRUNNERASYNCEXIT, Trace, "Exiting ActiveSessionStore.GetRunnerAsync, RunnerFound={RunnerFound}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetRunnerAsyncExit(this ILogger Logger, Boolean RunnerFound, String TraceIdentifier);

        [LoggerMessage(T_STOREREGRUNNER, Trace, "Enter ActiveSessionStore.RegisterRunnerInSession, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceRegisterRunnerInSession(this ILogger Logger, String SessionKey, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_STOREREGRUNNEREXIT, Trace, "Exit ActiveSessionStore.RegisterRunnerInSession, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceRegisterRunnerInSessionExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STOREUNREGRUNNER, Trace, "Enter ActiveSessionStore.UnregisterRunnerInSession, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerInSession(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(T_STOREUNREGRUNNERPERFORM, Trace, "Perorming the runner unregistration in the session, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTracePerformUnregisteration(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(T_STOREUNREGRUNNEREXIT, Trace, "Exit ActiveSessionStore.UnregisterRunnerInSession, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerInSessionExit(this ILogger Logger, String SessionKey, Int32 RunnerNumber);

        [LoggerMessage(T_STORERUNNERCACHE, Trace, "Enter ActiveSessionStore.ExtractRunnerFromCache, SessionKey=\"{SessionKey}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceExtractRunnerFromCache(this ILogger Logger, String SessionKey, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERCACHEFOUND, Trace, "The local runner is found in the cache, returning it, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceReturnRunnerFromCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERCACHENOTFOUND, Trace, "The local runner is not found in the cache, while being registered in the session (possibly evicted), unregister it, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceNoExpectedRunnerInCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERCACHEEXIT, Trace, "Exit ActiveSessionStore.ExtractRunnerFromCache, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceExtractRunnerFromCacheExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_SESSIONCONS, Trace, "Enter ActiveSession Constructor, SessionId=\"{SessionId}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionConstructor(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_SESSIONCONSEXIT, Trace, "Exit ActiveSession Constructor, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionConstructorExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_SESSIONNEWRUNNER, Trace, "Enter ActiveSession.CreateRunner, SessionId=\"{SessionId}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionCreateRunner(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_SESSIONNEWRUNNEREXIT, Trace, "Exit ActiveSession.CreateRunner, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceCreateActiveSessionCreateRunnerExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_SESSIONGETRUNNER, Trace, "Enter ActiveSession.GetRunner, SessionId=\"{SessionId}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionGetRunner(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_SESSIONGETRUNNEREXIT, Trace, "Exit ActiveSession.GetRunner, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionGetRunnerExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_SESSIONGETRUNNERASYNC, Trace, "Enter ActiveSession.GetRunnerAsync, SessionId=\"{SessionId}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionGetRunnerAsync(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_SESSIONGETRUNNERASYNCEXIT, Trace, "Exit ActiveSession.GetRunnerAsync, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionGetRunnerAsyncExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_SESSIONDISPOSE, Trace, "Disposing ActiveSession, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceActiveSessionDispose(this ILogger Logger, String SessionId);

        [LoggerMessage(T_FEATURECONS, Trace, "Enter ActiveSessionFeature constructor, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureConstructor(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECONSEXIT, Trace, "Exit ActiveSessionFeature constructor, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureConstructorExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECOMMIT, Trace, "Enter ActiveSessionFeature.CommitAsync, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureCommitAsync(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECOMMITAWAITSESSION, Trace, "Await for ActiveSession committing , TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureCommitAsyncActiveSessionWait(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECOMMITEXIT, Trace, "Exit ActiveSessionFeature.CommitAsync, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureCommitAsyncExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECLEAR, Trace, "Enter ActiveSessionFeature.Clear, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureClear(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECLEARSESSION, Trace, "Clearing reference to the ActiveSession, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeaturePerformClear(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECLEAREXIT, Trace, "Exit ActiveSessionFeature.Clear, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureClearExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADASYNC, Trace, "Enter ActiveSessionFeature.LoadAsync, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadAsync(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADASYNCSESSION, Trace, "Perform ActiveSessionFeature.ActiveSession property initialization, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncLoading(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADASYNCISESSIONLOAD, Trace, "Await for Session loading, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncSessionWait(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADASYNCISESSIONLOADED, Trace, "The Session has been loaded, proceed, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncSessionWaitEnded(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADASYNCSESSIONGET, Trace, "Create or fetch from cache the ActiveSession object, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncGetActiveSession(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADASYNCEXIT, Trace, "Exit ActiveSessionFeature.LoadAsync, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOAD, Trace, "Enter ActiveSessionFeature.Load, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoad(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADSESSION, Trace, "Load Session synchronously and create or fetch from cache the ActiveSession object, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadGetActiveSession(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADEXIT, Trace, "Exit ActiveSessionFeature.Load, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureLoadExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURESETSESSION, Trace, "Enter ActiveSessionFeature.SetSession, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureSetSession(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURESETSESSIONEXIT, Trace, "Exit ActiveSessionFeature.SetSessiond, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionFeatureSetSessionExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_DLGTFACTORYCONS, Trace, "Creating delegate runner factory to implement IActiveSessionRunnerFactory<{TRequest}, {TResult}>.")]
        public static partial void LogTraceConstructDelegateFactory(this ILogger Logger, String TRequest, String TResult);

        [LoggerMessage(T_DLGTFACTORYINVOKE, Trace, "Invoke delegate runner factory implementing IActiveSessionRunnerFactory<{TRequest}, {TResult}>.")]
        public static partial void LogTraceInvokingDelegateFactory(this ILogger Logger, String TRequest, String TResult);

        [LoggerMessage(T_TYPEFACTORCONS, Trace, "Creating type-based runner factory to implement IActiveSessionRunnerFactory<{TRequest}, {TResult}> via {ImplementingClassName}.")]
        public static partial void LogTraceConstructTypeFactory(this ILogger Logger, String TRequest, String TResult, String ImplementingClassName);

        [LoggerMessage(T_TYPEFACTORYINVOKE, Trace, "Invoke type-based runner factory implementing IActiveSessionRunnerFactory<{TRequest}, {TResult}>.")]
        public static partial void LogTraceInvokingTypeFactory(this ILogger Logger, String TRequest, String TResult);

        [LoggerMessage(T_MANAGERREGACQUIRING, Trace, "DefaultRunnerManager.RegisterRunner entered, acquiring runners lock SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRegisterRunnerEnter(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERREGACQUIRED, Trace, "DefaultRunnerManager.RegisterRunner: runners lock acquired, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRegisterRunnerLockAcquired(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERREGRELEASED, Trace, "DefaultRunnerManager.RegisterRunner exited, runners lock released, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRegisterRunnerExit(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERUNREGACQUIRING, Trace, "DefaultRunnerManager.UnregisterRunner entered, acquiring runners lock, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunner(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERUNREGACQUIRED, Trace, "DefaultRunnerManager.UnregisterRunner runners lock acquired, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerLockAcquired(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERUNREGPERFORM, Trace, "DefaultRunnerManager.UnregisterRunner, performing the runner unregistartion and cleanup SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerRemove(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERUNREGPREPTASK, Trace, "DefaultRunnerManager, preparing task for disposal of the runner, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTracePrepareDisposeTask(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERUNREGDISPASYNC, Trace, "DefaultRunnerManager, run a task returned by DisposeAsync(), SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunAsyncDisposeTask(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERUNREGDISPTASK, Trace, "DefaultRunnerManager, run Dispose() in a separate task, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunDisposeTask(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERUNREGNODISP, Trace, "DefaultRunnerManager, no disposal needed, run no task, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceRunNoDisposeTask(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERUNREGEXIT, Trace, "DefaultRunnerManager.UnregisterRunner exited, runners lock released, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerExit(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERNUMGET, Trace, "Enter DefaultRunnerManager.GetNewRunnerNumber, SessionId=\"{SessionId}\", TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetNewRunnerNumber(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_MANAGERNUMGETEXIT, Trace, "Exit DefaultRunnerManager.GetNewRunnerNumber returning the number, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceGetNewRunnerNumberExit(this ILogger Logger, String SessionId, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_MANAGERNUMRETURN, Trace, "DefaultRunnerManager: Return back unused runner number, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceReturnRunnerNumber(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERINFOACQUIRING, Trace, "DefaultRunnerManager.GetRunnerInfo entered, acquiring runners lock, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceGetRunnerInfo(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERINFOACQUIRED, Trace, "DefaultRunnerManager.GetRunnerInfo runners lock acquired, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceGetRunnerInfoLockAcquired(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERINFORELEASED, Trace, "DefaultRunnerManager.GetRunnerInfo exited, runners lock released, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}, RunnerInfo found: {Found}.")]
        public static partial void LogTraceGetRunnerInfoExit(this ILogger Logger, String SessionId, Int32 RunnerNumber, Boolean Found);

        [LoggerMessage(T_MANAGERDISPENDACQUIRING, Trace, "DefaultRunnerManager.FinishDisposalTask entered, acquiring runners lock, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceFinishDisposalTask(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERDISPENDACQUIRED, Trace, "DefaultRunnerManager.FinishDisposalTask runners lock acquired, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceFinishDisposalTaskLockAcquired(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERDISPENDRELEASED, Trace, "DefaultRunnerManager.FinishDisposalTask exited, runners lock released, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}")]
        public static partial void LogTraceFinishDisposalTaskExit(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERABORTALLACQUIRING, Trace, "DefaultRunnerManager.AbortAll entered, acquiring runners lock, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceAbortAll(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERABORTALLACQUIRED, Trace, "DefaultRunnerManager.AbortAll entered, runners lock acquired, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceAbortAllLockAcqired(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERABORTALLABORTING, Trace, "DefaultRunnerManager.AbortAll, aborting a runner, SessionId=\"{SessionId}\", RunnerNumber={RunnerNumber}")]
        public static partial void LogTraceAbortAllAbortRunner(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERABORTALLRELEASED, Trace, "DefaultRunnerManager.AbortAll exited, runners lock released, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceAbortAllExit(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPACQUIRED, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync started, runners lock acquired, SessionId=\"{SessionId}\".")]
        public static partial void LogTracePerformRunnersCleanup(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPRELEASED, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync, runners lock released, awaiting CleanupRunners , SessionId=\"{SessionId}\".")]
        public static partial void LogTracePerformRunnersCleanupAwaiting(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPDISPOSING, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync disposing this DefaultRunnerManger instance, SessionId=\"{SessionId}\".")]
        public static partial void LogTracePerformRunnersCleanupDisposing(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPENDED, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync finished, SessionId=\"{SessionId}\".")]
        public static partial void LogTracePerformRunnersCleanupComplete(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLTASK, Trace, "DefaultRunnerManager.CleanupRunners entered, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceCleanupRunners(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLTASKWAIT, Trace, "DefaultRunnerManager.CleanupRunners waiting for all runners to unregister, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceCleanupRunnersWaitForUnregistration(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLTASKACQUIRING, Trace, "DefaultRunnerManager.CleanupRunners acquiring runners lock, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceCleanupRunnersAcquiringLock(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLTASKACQUIRED, Trace, "DefaultRunnerManager.CleanupRunners runners lock acquired, SessionId=\"{SessionId}\".")]
        public static partial void LogTraceCleanupRunnersLockAcquired(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLTASKRELEASED, Trace, "DefaultRunnerManager.CleanupRunners runners lock released, returning a task awaiting disposing all runners, SessionId=\"{SessionId}\".")]
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
