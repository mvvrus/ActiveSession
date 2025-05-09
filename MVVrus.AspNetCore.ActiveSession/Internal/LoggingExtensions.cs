﻿using Microsoft.Extensions.Caching.Memory; 
using System;
using System.Text;
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

        [LoggerMessage(E_FAILCREATERUNNER, Error, "The factory failed to create a runner and returned null, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogErrorCreateRunnerFailure(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(E_REMOTERUNNER, Error, "Using remote runners is not allowed by configuration setting ThrowOnRemoteRunner, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogErrorRemoteRunnerUnavailable(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(E_NORUNNERNUMBER, Error, "Cannot acquire a new runner number, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogErrorCannotAllocateRunnerNumber(this ILogger Logger, String SessionId, String TraceIdentifier);


        [LoggerMessage(W_NOSTORE, Warning, "Cannot obtain an ActiveSession store service from the application service container. The ActiveSession middleware cannot be registered. Was the IServiceCollection.AddActiveServices extension method called at least once?")]
        public static partial void LogWarningAbsentFactoryInplementations(this ILogger Logger, Exception AnException);

        [LoggerMessage(W_NOOWNCACHE, Warning, "ActiveSessionStore constructor: cannot create our own cache, fall back to the shared cache.")]
        public static partial void LogWarningActiveSessionStoreCannotCreateOwnCache(this ILogger Logger, Exception AnException);

        [LoggerMessage(W_STORERUNNERCREATIONFAIL, Warning, "Exit ActiveSessionStore.CreateRunner due to the exception, the cache entry has been removed, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogWarningCreateRunnerFailure(this ILogger Logger, Exception AnException, String SessionId, String TraceIdentifier);

        [LoggerMessage(W_NONRUNNERTYPE, Warning, "Something found in the local cache but but it is not a runner, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogWarningNotRunnerInCache(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(W_ACTIVESESSIONDISPOSEEXCEPTION, Warning, "Exception occured while disposing an the active session object: ActiveSessionId={SessionId}.")]
        public static partial void LogWarningExceptionWhileActiveSessionDispose(this ILogger Logger, Exception Exception, String SessionId);
        
        [LoggerMessage(W_INCOMPATRUNNERTYPE, Warning, "The runner is found in the local cache but cannot be returned: the runner type is incompatible, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogWarningNoExpectedRunnerInCache(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(W_REMOTENOTIMPLMENTED, Warning, "Accessing the remote runner is not implemented, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogWarningRemoteRunnerUnavailable(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(W_ACTIVESESSIONFAIL, Warning, "The exception occered while establishing ActiveSessionFeature.ActiveSession property, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogWarningActiveSessionLoad(this ILogger Logger, Exception exception, String TraceIdentifier);

        [LoggerMessage(W_REGISTERDURINGCLEANUP, Warning, "Attempt to register the runner after it's Active Session cleanup initiation, an exception to be throwed, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogWarningRegisterRunnerAfterCleanupInit(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);


        [LoggerMessage(I_MIDDLEWAREREGISTERED, Information, "ActiveSession middleware is registered for addition to a middleware pipeline.")]
        public static partial void LogInformationActiveSessionMiddlewareRegistered(this ILogger Logger);

        [LoggerMessage(I_MIDDLEWAREADDED, Information, "ActiveSession middleware is added to the middleware pipeline.")]
        public static partial void LogInformationActiveSessionMiddlewareAdded(this ILogger Logger);

        [LoggerMessage(I_STOREINCOSNSISTENTTERMINATION, Information, "Terminating a session with inconsistent generation {Generation}(current) vs {SessGeneration}(in ASP.NET Core session), ActiveSession.Id={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogInfoInconsistentSessionTermination(this ILogger Logger, Int32 Generation, Int32 SessGeneration, String SessionId, String TraceIdentifier);

        [LoggerMessage(I_STORECONSTRUCTORCACHECREATED, Information, "ActiveSessionStore constructor created its own cache.")]
        public static partial void LogDebugActiveSessionStoreConstructorOwnCaheCreated(this ILogger Logger);

        [LoggerMessage(I_STORERUNNERFACTORYNEW, Information, "The runner factory was created and is to be added to the cache, TRequest={TRequest}, TResult={TResult}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogInfoInstatiateNewRunnerFactory(this ILogger Logger, String TRequest, String TResult, String TraceIdentifier);

        [LoggerMessage(I_STOREGETRUNNERCLEANUPVARS, Information, "A runner specified is not registered in the ASP.NET Core session, cannot proceed, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogInformationNoRunnerInSession(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);


        [LoggerMessage(D_FEATUREACTIVATED, Debug, "ActiveSession feature is added to the request context with suffix:{Suffix}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogDebugActiveSessionFeatureActivated(this ILogger Logger, String Suffix, String TraceIdentifier);

        [LoggerMessage(D_SERVICESSUBSTITUTED, Debug, "Original RequestServices are substituted by the ActiveSession.SessionServices , TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogDebugRequestServicesChangedToSessionServices(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(D_ADDMIDLWAREFILTER, Debug, "New middleware filter added: {Name}:{Order}.")]
        public static partial void LogDebugAddNewFilter(this ILogger Logger, String Name, Int32 Order);

        [LoggerMessage(D_GROUPMIDLWAREFILTER, Debug, "The source {Name}:{Order} was groupped with the existing middleware group filter: {GroupName}:{GroupOrder}.")]
        public static partial void LogDebugGroupFilter(this ILogger Logger, String Name, Int32 Order, String GroupName, Int32 GroupOrder);

        [LoggerMessage(D_MIDLWAREMAPPINGFINISHED, Debug, "Mappig request to the set of middleware filters finished, WasMapped={WasMapped}, Suffix={Suffix}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogDebugMappingFinished(this ILogger Logger, Boolean WasMapped, String Suffix, String TraceIdentifier);

        [LoggerMessage(D_STORECONSTRUCTOROPTIONS, Debug, "ActiveSessionStore constructor will use the follwing options: {Options}.")]
        public static partial void LogDebugActiveSessionStoreConstructorOptions(this ILogger Logger, ActiveSessionOptions Options);

        [LoggerMessage(D_STORECACHEOPTIONS, Debug, "ActiveSessionStore constructor will use the follwing options for its own cache: {Options}.")]
        public static partial void LogDebugActiveSessionStoreConstructorOwnCacheOptions(this ILogger Logger, MemoryCacheOptionsForLogging Options);

        [LoggerMessage(D_STORESESSIONFOUNDTERMINATED, Debug, "Found existing ActiveSession that is outdated, terminate it and return null instead, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogDebugFoundTerminatedActiveSession(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(D_STORESESSIONTOCREATE, Debug, "Creating new ActiveSession, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogDebugCreateNewActiveSession(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(D_STORESESSIONEXCEPTIONEXIT, Debug, "Exit ActiveSessionStore.FetchOrCreateSession due to the exception, the cache entry has been removed, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogDebugFetchOrCreateExceptionalExit(this ILogger Logger, Exception AnException, String SessionId, String TraceIdentifier);

        [LoggerMessage(D_STORESESSIONEVICTED, Debug, "ActiveSession was evicted fom the store and to be disposed, ActiveSessionId={SessionId}.")]
        public static partial void LogDebugSessionEvicted(this ILogger Logger, String SessionId);

        [LoggerMessage(D_STORERUNNERCLEANUPRESULT, Debug, "Observing timely completion of the session cleanup, ActiveSessionId={SessionId}, WithinTimeout={WaitSucceeded}.")]
        public static partial void LogDebugActiveSessionEndWaitingForCleanup(this ILogger Logger, String SessionId, Boolean WaitSucceeded);

        [LoggerMessage(D_STORERUNNERPROXY, Debug, "Trying to make a proxy for the remote runner, RunnerId={RunnerId}, HostId={HostId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogDebugProcessRemoteRunner(this ILogger Logger, RunnerId RunnerId, String HostId, String TraceIdentifier);

        [LoggerMessage(D_FEATURELOADED, Debug, "ActiveSessionFeature, the active session feature loaded, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogDebugActiveSessionFeatureLoaded(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(D_FEATUREREFRESHED, Debug, "ActiveSessionFeature, the active session changed, old ActiveSessionId={OldSessionId}, new ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogDebugActiveSessionFeatureChanged(this ILogger Logger, String OldSessionId, String SessionId, String TraceIdentifier);

        [LoggerMessage(D_MANAGERRUNNERREGISTERED, Debug, "The runner is registered and available for execution, RunnerId={RunnerId}, , TraceIdentifier={TraceIdentifier}.")]
        public static partial void  LogDebugNewRunnerAvailable(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(D_MANAGERRUNNERNOTREGISTERED, Debug, "The runner to be unregistered is not registered, RunnerId={RunnerId}.")]
        public static partial void LogDebugUnregisterRunnerNotRegistered(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(D_MANAGERRUNNERRUNEGISTERED, Debug, "The runner executon ended, it is unregistered and ready to cleanup, RunnerId={RunnerId}.")]
        public static partial void LogDebugRunnerExecutionEnded(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(D_MANAGERRUNNERCLEANUPWAITFINISHED, Debug, "Observing the runner cleanup finished, RunnerId={RunnerId}, WithinTimeout={WaitSucceeded}.")]
        public static partial void LogDebugRunnerEndWaitingForCleanup(this ILogger Logger, RunnerId RunnerId, Boolean WaitSucceeded);

#if TRACE
        [LoggerMessage(T_BUILDUSE, Trace, "Enetering ActiveSessionBuilderExtensions.UseActiveSessions.")]
        public static partial void LogTraceUseActiveSessions(this ILogger Logger);

        [LoggerMessage(T_BUILDUSEEXTRACTPARAM, Trace, "ActiveSessionBuilderExtensions.UseActiveSessions: extracting the middleware constructor parameter.")]
        public static partial void LogTraceUseActiveSessionCreateNewParams(this ILogger Logger);

        [LoggerMessage(T_BUILDUSECREATEPARAM, Trace, "ActiveSessionBuilderExtensions.UseActiveSessions: creating new middleware constructor parameter.")]
        public static partial void LogTraceUseActiveSessionExtractExistingParams(this ILogger Logger);

        [LoggerMessage(T_BUILDUSEADDCATCHALL, Trace, "ActiveSessionBuilderExtensions.UseActiveSessions: mark middleware constructor parameter as catch-all.")]
        public static partial void LogTraceUseActiveSessionParamsMarkCatchAll(this ILogger Logger);

        [LoggerMessage(T_BUILDUSEADDFILTER, Trace, "ActiveSessionBuilderExtensions.UseActiveSessions: add a filter to the middleware constructor parameter.")]
        public static partial void LogTraceUseActiveSessionParamsAddFilter(this ILogger Logger);

        [LoggerMessage(T_BUILDUSEEXIT, Trace, "Exiting ActiveSessionBuilderExtensions.UseActiveSessions.")]
        public static partial void LogTraceUseActiveSessionsExit(this ILogger Logger);

        [LoggerMessage(T_MIDDLEWARECONS, Trace, "Enetering ActiveSessionMiddleware constructor.")]
        public static partial void LogTraceConstructActiveSessionMiddleware(this ILogger Logger);

        [LoggerMessage(T_MIDDLEWARECONSEXIT, Trace, "Exiting ActiveSessionMiddleware constructor.")]
        public static partial void LogTraceConstructActiveSessionMiddlewareExit(this ILogger Logger);

        [LoggerMessage(T_MIDDLEWAREENTER, Trace, "Entering ActiveSessionMiddleware, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceInvokeActiveSessionMiddleware(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWAREADDFEATURE, Trace, "The request did pass criteria for assigning an ActiveSession (suffix: {Suffix}), TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionMiddlewareAssignFeature(this ILogger Logger, String Suffix, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWAREDONTADDFEATURE, Trace, "The request did not pass criteria for assigning an ActiveSession, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionMiddlewareAssignNoFeature(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWARELOADAWAIT, Trace, "Awaiting while loading ActiveSession for SessionServices, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceWaitingForActiveSessionLoading(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWARESERVICESSUBSTITUTED, Trace, "Completed attempt to substituste RequestServices by SessionServices, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceCompleteRequestServicesSubstitutionAttempt(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWARENEXTINVOKE, Trace, "Invoking the rest of the middleware pipeline after the ActiveSession middleware, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionMiddlewareInvokeRest(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWARENEXTRETURN, Trace, "Control from the rest of the pipeline was returned without exceptions, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionMiddlewareControlReturns(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWARENEXTEXCEPTION, Trace, "An exception in the the pipeline has been caught, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTracePipelineException(this ILogger Logger, Exception AnException, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWAREEXIT, Trace, "ActiveSession middleware exited, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionMiddlewareExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWAREMAPPINGADDFILTER, Trace, "Adding filter from source {Name}:{Order}.")]
        public static partial void LogTraceMiddlewareMapperAddFilter(this ILogger Logger, String Name, Int32 Order);

        [LoggerMessage(T_MIDDLEWAREMAPPINGSTART, Trace, "Mappig request to the set of middleware filters started, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceMiddlewareMapper(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWAREMAPPINGAPPLIED, Trace, "A filter {FilterName} was appied, WasMapped={WasMapped}, Suffix={Suffix}, Order={Order}, PrevOrder={PrevOrder}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceMiddlewareMapperFilterApplied(this ILogger Logger, String FilterName, Boolean WasMapped, String Suffix, Int32 Order, Int32? PrevOrder, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWAREMAPPINGHIGHERPRIORITY, Trace, "A filter with higher order that accepts the request is found, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceMiddlewareMapperHigherPriority(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWAREMAPPINGSET, Trace, "A mapping is done by this filter, Suffix={Suffix}, Order={Order}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceMiddlewareMapperSetMapping(this ILogger Logger, String Suffix, Int32 Order, String TraceIdentifier);

        [LoggerMessage(T_MIDDLEWAREMAPPINGSUFFIXONLY, Trace, ", Suffix={Suffix}, Order={Order}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceMiddlewareMapperSetSuffixOnly(this ILogger Logger, String Suffix, Int32 Order, String TraceIdentifier);

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

        [LoggerMessage(T_STORESESSIONKEY, Trace, "Base activeSession ID to use:{SessionId}:(no generation yet), TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceBaseActiveSessionKeyToUse(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORESESSION, Trace, "Eneter ActiveSessionStore.FetchOrCreateSession, ActiveSessionId={SessionId}:(no generation yet), TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceFetchOrCreate(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORESESSIONTERMINATED, Trace, "ActiveSessionStore.FetchOrCreateSession: the ActiveSession found is outdated, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceFetchOrCreateOutdatedSessionFound(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORESESSIONFOUND, Trace, "Found existing ActiveSession, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceFoundExistingActiveSession(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORESESSIONACQUIRING, Trace, "ActiveSessionStore.FetchOrCreateSession: acquiring session lock, ActiveSessionId={SessionId}:(no generation yet), TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceAcquiringSessionCreationLock(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORESESSIONACQUIRED, Trace, "ActiveSessionStore.FetchOrCreateSession: acquired session lock, ActiveSessionId={SessionId}:(no generation yet), TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceAcquiredSessionCreationLock(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORESESSIONRELEASED, Trace, "ActiveSessionStore.FetchOrCreateSession: released session lock, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceReleasedSessionCreationLock(this ILogger Logger, String SessionId, String TraceIdentifier); 

        [LoggerMessage(T_STORESESSIONEXIT, Trace, "Exit ActiveSessionStore.FetchOrCreateSession, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceFetchOrCreateExit(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORECLEANUPOUTDATED, Trace, "Starting cleanup of outdated runner vars in the Session, ActiveSessionId={SessionId}:(any generations), TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceCleanupOutdatedRunnerVars(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORECLEANUPOUTDATEDREMOVE, Trace, "Removing outdated item, Key={Key}, ActiveSessionId={SessionId}:(any generations), TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceCleanupOutdatedRunnerVarsRemove(this ILogger Logger, String Key, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORECLEANUPOUTDATEDEXIT , Trace, "Finished cleanup of outdated runner vars in the Session, ActiveSessionId={SessionId}:(any generations), TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceCleanupOutdatedRunnerVarsExit(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORESESSIONCALLBACK, Trace, "Enter ActiveSession eviction callback, acquiring session lock, ActiveSessionId={SessionId}")]
        public static partial void LogTraceSessionEvictionCallback(this ILogger Logger, String SessionId);

        [LoggerMessage(T_STORESESSIONCALLBACKLOCKED, Trace, "ActiveSession eviction callback: session lock acquired, ActiveSessionId={SessionId}")]
        public static partial void LogTraceSessionEvictionCallbackLocked(this ILogger Logger, String SessionId);

        [LoggerMessage(T_STORESESSIONCALLBACKUNLOCKED, Trace, "ActiveSession eviction callback: session lock released, ActiveSessionId={SessionId}")]
        public static partial void LogTraceSessionEvictionCallbackUnlocked(this ILogger Logger, String SessionId);

        [LoggerMessage(T_STORESESSIONCALLBACKABORTALL, Trace, "Abort all runners of the evicted session, ActiveSessionId={SessionId}")]
        public static partial void LogTraceAbortRunners(this ILogger Logger, String SessionId);

        [LoggerMessage(T_STORESESSIONCALLBACKDISPOSINGSESSION, Trace, "Disposing ActiveSession after eviction, ActiveSessionId={SessionId}")]
        public static partial void LogTraceDisposingActiveSession(this ILogger Logger, String SessionId);

        [LoggerMessage(T_STORESESSIONCALLBACKCLEANUPSTART, Trace, "Cleanup of all runners for the ActiveSession is initiated, ActiveSessionId={SessionId}")]
        public static partial void LogTraceSessionCleanupRunnersInitiated(this ILogger Logger, String SessionId);

        [LoggerMessage(T_STORESESSIONCALLBACKDISPOSINGSCOPE, Trace, "The ActiveSession service container scope to be disposed, ActiveSessionId={SessionId}")]
        public static partial void LogTraceSessionScopeToBeDisposed(this ILogger Logger, String SessionId);

        [LoggerMessage(T_STORESESSIONCALLBACKEXIT, Trace, "Exit ActiveSession EvictionCallback, ActiveSessionId={SessionId}")]
        public static partial void LogTraceSessionEvictionCallbackExit(this ILogger Logger, String SessionId);

        [LoggerMessage(T_STORENEWRUNNER, Trace, "Enter ActiveSessionStore.CreateRunner, try to use session-level runner creation lock, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceCreateRunner(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNERTOCREATE, Trace, "New runner to be created, ActiveSessionId={SessionId}, Generation={Generation}, RunnerNumber={RunnerNumber}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceNewRunnerInfoRunner(this ILogger Logger, String SessionId, Int32 Generation, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERCREATED, Trace, "A new runner was created, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceCreateNewRunner(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNERWAIVENUMBER, Trace, "Waive to use the runner number previousely acquired because of an exception,  ActiveSessionId={SessionId}, RunnerNumber={RunnerNumber}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceWaiveRunnerNumber(this ILogger Logger, String SessionId, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNERLOCKFALLBACK, Trace, "Fallback to store-level lock as runner creation lock, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceFallbackToStoreGlobalLock(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNERACQUIRING, Trace, "Acquiring the runner creation lock for session {SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceAcquiringRunnerCreationLock(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNERACQUIRED, Trace, "Acquired the runner creation lock for session {SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceAcquiredRunnerCreationLock(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNERRELEASED, Trace, "Released the runner creation lock for session {SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceReleasedRunnerCreationLock(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORENEWRUNNEREXIT, Trace, "Exiting ActiveSessionStore.CreateRunner, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceCreateRunnerExit(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERFACTORY, Trace, "Enter ActiveSessionStore.GetRunnerFactory, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceGetRunnerFactory(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERFACTORYFOUND, Trace, "The runner factory was fetched from the cache, TRequest={TRequest}, TResult={TResult}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceGetRunnerFactoryFromCache(this ILogger Logger, String TRequest, String TResult, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERFACTORYCACHE, Trace, "Storing the factory in the cache, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceStoreNewRunnerFactoryInCache(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERFACTORYEXIT, Trace, "Exiting ActiveSessionStore.GetRunnerFactory, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceGetRunnerFactoryExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERCALLBACK, Trace, "The ActiveSessionStore.RunnerEvictionCallback entered, RunnerId={RunnerId}.")]
        public static partial void LogTraceRunnerEvictionCallback(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_STORERUNNERCALLBACKEXIT, Trace, "The ActiveSessionStore.RunnerEvictionCallback exited, RunnerId={RunnerId}.")]
        public static partial void LogTraceRunnerEvictionCallbackExit(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_STORERUNNERCLEANUP, Trace, "Starting task observing completion of all runners for the ActiveSession, ActiveSessionId={SessionId}.")]
        public static partial void LogTraceActiveSessionCompleteDispose(this ILogger Logger, String SessionId);

        [LoggerMessage(T_STOREGETRUNNER, Trace, "Entering ActiveSessionStore.GetRunner, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceGetRunner(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERFOUND, Trace, "Extracting the local runner from cache, RunnerId={RunnerId}, , TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceGetLocalRunnerFromCache(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_STOREGETRUNNEREXIT, Trace, "Exiting ActiveSessionStore.GetRunner, RunnerId={RunnerId}, RunnerFound={RunnerFound}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceGetRunnerExit(this ILogger Logger, RunnerId RunnerId, Boolean RunnerFound, String TraceIdentifier);

        [LoggerMessage(T_STOREGETRUNNERASYNC, Trace, "Enter ActiveSessionStore.GetRunnerAsync, awaiting until the session is loaded, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceGetRunnerAsync(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_STOREGETRUNNERASYNCLOADED, Trace, "The session is loaded and the runner info is known now: RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceGetRunnerAsyncInfoRunner(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_STOREGETRUNNERASYNCWAITPROXY, Trace, "Awaiting until the proxy is created, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceAwaitForProxyCreation(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_STOREGETRUNNERASYNCEXIT, Trace, "Exiting ActiveSessionStore.GetRunnerAsync, RunnerId={RunnerId}, RunnerFound={RunnerFound}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceGetRunnerAsyncExit(this ILogger Logger, RunnerId RunnerId, Boolean RunnerFound, String TraceIdentifier);

        [LoggerMessage(T_STOREREGRUNNER, Trace, "Enter ActiveSessionStore.RegisterRunnerInSession, SessionId={SessionId}, RunnerKey={RunnerKey}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceRegisterRunnerInSession(this ILogger Logger, String SessionId, String RunnerKey, String TraceIdentifier);

        [LoggerMessage(T_STOREREGRUNNEREXIT, Trace, "Exit ActiveSessionStore.RegisterRunnerInSession, SessionId={SessionId}, RunnerKey={RunnerKey}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceRegisterRunnerInSessionExit(this ILogger Logger, String SessionId, String RunnerKey, String TraceIdentifier);

        [LoggerMessage(T_STOREUNREGRUNNER, Trace, "Enter ActiveSessionStore.UnregisterRunnerInSession, SessionId={SessionId}, RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerInSession(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_STOREUNREGRUNNERPERFORM, Trace, "Perorming the runner unregistration in the ASP.NET Core session, SessionId={SessionId}, RunnerNumber={RunnerNumber}.")]
        public static partial void LogTracePerformUnregisteration(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_STOREUNREGRUNNEREXIT, Trace, "Exit ActiveSessionStore.UnregisterRunnerInSession, SessionId={SessionId}, RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceUnregisterRunnerInSessionExit(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_STORERUNNERCACHE, Trace, "Enter ActiveSessionStore.ExtractRunnerFromCache, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceExtractRunnerFromCache(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERCACHEFOUND, Trace, "The local runner is found in the cache, returning it, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceReturnRunnerFromCache(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERCACHENOTFOUND, Trace, "The local runner is not found in the cache, while being registered in the session (possibly evicted), unregister it, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceNoExpectedRunnerInCache(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_STORERUNNERCACHEEXIT, Trace, "Exit ActiveSessionStore.ExtractRunnerFromCache, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceExtractRunnerFromCacheExit(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_STORETERMINATE, Trace, "Enter ActiveSessionStore.TerminateSession, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceSessionTerminate(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORETERMINATEALREADY, Trace, "ActiveSessionStore.TerminateSession  already terminated, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceSessionAlreadyTerminated(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STORETERMINATEEXIT, Trace, "Exit ActiveSessionStore.TerminateSession, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceSessionTerminateExit(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STOREDOTERMINATE, Trace, "Enter ActiveSessionStore.DoTerminateSession, acquiring session lock, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceSessionDoTerminate(this ILogger Logger, String SessionId, String TraceIdentifier);
        
        [LoggerMessage(T_STOREDOTERMINATELOCKED, Trace, "ActiveSessionStore.DoTerminateSession: session lock acquired, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceSessionDoTerminateLockAcquired(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STOREDOTERMINATEVIAEVICT, Trace, "ActiveSessionStore.DoTerminateSession: terminate via eviction from the cache, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceSessionDoTerminateViaEvict(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STOREDOTERMINATEUNLOCKED, Trace, "ActiveSessionStore.DoTerminateSession: session lock released, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceSessionDoTerminateLockReleased(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STOREDOTERMINATEVIADISPOSE, Trace, "ActiveSessionStore.DoTerminateSession: not present in the cache means already terminated, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceSessionDoTerminateDisposeEvicted(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STOREDOTERMINATEEXIT, Trace, "Exit ActiveSessionStore.DoTerminateSession, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceSessionDoTerminateExit(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STOREGROUPACCOUNTFORSESSION, Trace, "Count a refernce to the group object from the active session created/found, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceStoreGroupAccountForSession(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_STOREGROUPOBTAINREF, Trace, "Obtaining a reference to the active session group object, BaseId={BaseId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceObtainStoreGroupRef(this ILogger Logger, String BaseId, String TraceIdentifier);

        [LoggerMessage(T_STOREGROUPCREATENEW, Trace, "Creating a new active session group, BaseId={BaseId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceCreateStoreGroup(this ILogger Logger, String BaseId, String TraceIdentifier);

        [LoggerMessage(T_STOREGROUPOBTAINREFEXIT, Trace, "Obtaining a reference to the active session group object - exit, BaseId={BaseId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceObtainStoreGroupRefExit(this ILogger Logger, String BaseId, String TraceIdentifier);

        [LoggerMessage(T_STOREGROUPRELEASEREF, Trace, "Releasing a reference to the active session group object, BaseId={BaseId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceReleaseStoreGroupRef(this ILogger Logger, String BaseId, String TraceIdentifier);

        [LoggerMessage(T_STOREGROUPREMOVE, Trace, "Removing the unreferenced active session group object from the list, BaseId={BaseId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceRemoveStoreGroup(this ILogger Logger, String BaseId, String TraceIdentifier);

        [LoggerMessage(T_STOREGROUPRELEASEREFEXIT, Trace, "Releasing a reference to the active session group object - exit, BaseId={BaseId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceReleaseStoreGroupRefExit(this ILogger Logger, String BaseId, String TraceIdentifier);

        [LoggerMessage(T_STOREGROUPDETACHSESSION, Trace, "Notifying that the active session object reference is not used anymore - acquiring lock, ActiveSessionId={SessionId}, BaseId={BaseId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceStoreDetachSession(this ILogger Logger, String SessionId, String BaseId, String TraceIdentifier);

        [LoggerMessage(T_STOREGROUPDETACHSESSIONLOCKACQUIRED, Trace, "Notifying that the active session object reference is not used anymore - lock acquired, ActiveSessionId={SessionId}, BaseId={BaseId}, TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceStoreDetachSessionLockAcqired(this ILogger Logger, String SessionId, String BaseId, String TraceIdentifier);

        [LoggerMessage(T_STOREGROUPDETACHSESSIONEXIT, Trace, "Notifying that the active session object reference is not used anymore - lock released, exit, ActiveSessionId={SessionId},BaseId={BaseId},  TraceIdentifier={TraceIdentifier}. ")]
        public static partial void LogTraceStoreDetachSessionExit(this ILogger Logger, String SessionId, String BaseId, String TraceIdentifier);

        [LoggerMessage(T_SESSIONCONS, Trace, "Enter ActiveSession Constructor, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionConstructor(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_SESSIONCONSEXIT, Trace, "Exit ActiveSession Constructor, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionConstructorExit(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_SESSIONNEWRUNNER, Trace, "Enter ActiveSession.CreateRunner, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionCreateRunner(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_SESSIONNEWRUNNEREXIT, Trace, "Exit ActiveSession.CreateRunner, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceCreateActiveSessionCreateRunnerExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_SESSIONGETRUNNER, Trace, "Enter ActiveSession.GetRunner, ActiveSessionId={SessionId}, RunnerNumber={RunnerNumber}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionGetRunner(this ILogger Logger, String SessionId, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_SESSIONGETRUNNEREXIT, Trace, "Exit ActiveSession.GetRunner, ActiveSessionId={SessionId}, RunnerNumber={RunnerNumber}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionGetRunnerExit(this ILogger Logger, String SessionId, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_SESSIONGETRUNNERNOTYPE, Trace, "Enter ActiveSession.GetNonTypedRunner, ActiveSessionId={SessionId}, RunnerNumber={RunnerNumber}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionGetNonTypedRunner(this ILogger Logger, String SessionId, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_SESSIONGETRUNNERNOTYPEEXIT, Trace, "Exit ActiveSession.GeNonTypedRunner, ActiveSessionId={SessionId}, RunnerNumber={RunnerNumber}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionGetNonTypedRunnerExit(this ILogger Logger, String SessionId, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_SESSIONGETRUNNERASYNC, Trace, "Enter ActiveSession.GetRunnerAsync, ActiveSessionId={SessionId}, RunnerNumber={RunnerNumber}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionGetRunnerAsync(this ILogger Logger, String SessionId, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_SESSIONGETRUNNERASYNCEXIT, Trace, "Exit ActiveSession.GetRunnerAsync, ActiveSessionId={SessionId}, RunnerNumber={RunnerNumber}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionGetRunnerAsyncExit(this ILogger Logger, String SessionId, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_SESSIONGETRUNNERNOTYPEASYNC, Trace, "Enter ActiveSession.GetNonTypedRunnerAsync, ActiveSessionId={SessionId}, RunnerNumber={RunnerNumber}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionGetNonTypedRunnerAsync(this ILogger Logger, String SessionId, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_SESSIONGETRUNNERNOTYPEASYNCEXIT, Trace, "Exit ActiveSession.GetNonTypedRunnerAsync, ActiveSessionId={SessionId}, RunnerNumber={RunnerNumber}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionGetNonTypedRunnerAsyncExit(this ILogger Logger, String SessionId, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_SESSIONDISPOSE, Trace, "Disposing ActiveSession, ActiveSessionId={SessionId}.")]
        public static partial void LogTraceActiveSessionDispose(this ILogger Logger, String SessionId);

        [LoggerMessage(T_SESSIONTERMINATE, Trace, "ActiveSession.Terminate called, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionTerminateCalled(this ILogger Logger, String SessionId, String TraceIdentifier);
        
        [LoggerMessage(T_SESSIONREQUESTSERVICELOCK, Trace, "ActiveSession: Acquiring service lock, ServiceName={ServiceName}, ActiveSessionId={SessionId}.")]
        public static partial void LogTraceActiveSessionRequestServiceLock(this ILogger Logger, String ServiceName, String SessionId);

        [LoggerMessage(T_SESSIONACQUIRESERVICELOCK, Trace, "ActiveSession: End of acquiring service lock, ServiceName={ServiceName}, ActiveSessionId={SessionId}, Acquired:{Acquired}.")]
        public static partial void LogTraceActiveSessionAcquireServiceLock(this ILogger Logger, String ServiceName, String SessionId, Boolean Acquired);

        [LoggerMessage(T_SESSIONRELEASESERVICELOCK, Trace, "ActiveSession: Releasing service lock, ServiceName={ServiceName}, ActiveSessionId={SessionId}.")]
        public static partial void LogTraceActiveSessionReleaseServiceLock(this ILogger Logger, String ServiceName, String SessionId);

        [LoggerMessage(T_FEATURECONS, Trace, "Enter ActiveSessionFeature constructor, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureConstructor(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECONSEXIT, Trace, "Exit ActiveSessionFeature constructor, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureConstructorExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECOMMIT, Trace, "Enter ActiveSessionFeature.CommitAsync, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureCommitAsync(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECOMMITAWAITSESSION, Trace, "Await for ActiveSession committing , TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureCommitAsyncActiveSessionWait(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECOMMITEXIT, Trace, "Exit ActiveSessionFeature.CommitAsync, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureCommitAsyncExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECLEAR, Trace, "Enter ActiveSessionFeature.Clear, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureClear(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECLEARSESSION, Trace, "Clearing reference to the ActiveSession, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeaturePerformClear(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURECLEAREXIT, Trace, "Exit ActiveSessionFeature.Clear, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureClearExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADASYNC, Trace, "Enter ActiveSessionFeature.LoadAsync, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureLoadAsync(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADASYNCSESSION, Trace, "Perform ActiveSessionFeature.ActiveSession property initialization, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncLoading(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADASYNCISESSIONLOAD, Trace, "Await for Session loading, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncSessionWait(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADASYNCISESSIONLOADED, Trace, "The Session has been loaded, proceed, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncSessionWaitEnded(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADASYNCSESSIONGET, Trace, "Create or fetch from cache the ActiveSession object, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncGetActiveSession(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADASYNCEXIT, Trace, "Exit ActiveSessionFeature.LoadAsync, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureLoadAsyncExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOAD, Trace, "Enter ActiveSessionFeature.Load, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureLoad(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADSESSION, Trace, "Load Session synchronously and create or fetch from cache the ActiveSession object, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureLoadGetActiveSession(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATURELOADEXIT, Trace, "Exit ActiveSessionFeature.Load, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureLoadExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATUREREFRESH, Trace, "Enter ActiveSessionFeature.RefreshActiveSession, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureRefresh(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_FEATUREREFRESHEXIT, Trace, "Exit ActiveSessionFeature.RefreshActiveSession, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceActiveSessionFeatureRefreshExit(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(T_DLGTFACTORYCONS, Trace, "Creating delegate runner factory to implement IRunnerFactory<{TRequest}, {TResult}>.")]
        public static partial void LogTraceConstructDelegateFactory(this ILogger Logger, String TRequest, String TResult);

        [LoggerMessage(T_DLGTFACTORYINVOKE, Trace, "Invoke delegate runner factory implementing IRunnerFactory<{TRequest}, {TResult}> for RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceInvokingDelegateFactory(this ILogger Logger, String TRequest, String TResult, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_TYPEFACTORCONS, Trace, "Creating type-based runner factory to implement IRunnerFactory<{TRequest}, {TResult}> via {ImplementingClassName}.")]
        public static partial void LogTraceConstructTypeFactory(this ILogger Logger, String TRequest, String TResult, String ImplementingClassName);

        [LoggerMessage(T_TYPEFACTORYINVOKE, Trace, "Invoke type-based runner factory implementing IRunnerFactory<{TRequest}, {TResult}> for RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceInvokingTypeFactory(this ILogger Logger, String TRequest, String TResult, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_MANAGERREGACQUIRING, Trace, "DefaultRunnerManager.RegisterRunner entered, acquiring runners lock, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceRegisterRunnerEnter(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_MANAGERREGACQUIRED, Trace, "DefaultRunnerManager.RegisterRunner: runners lock acquired, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceRegisterRunnerLockAcquired(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_MANAGERREGRELEASED, Trace, "DefaultRunnerManager.RegisterRunner exited, runners lock released, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceRegisterRunnerExit(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_MANAGERUNREGACQUIRING, Trace, "DefaultRunnerManager.UnregisterRunner entered, acquiring runners lock, RunnerId={RunnerId}.")]
        public static partial void LogTraceUnregisterRunner(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERUNREGACQUIRED, Trace, "DefaultRunnerManager.UnregisterRunner runners lock acquired, RunnerId={RunnerId}.")]
        public static partial void LogTraceUnregisterRunnerLockAcquired(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERUNREGPERFORM, Trace, "DefaultRunnerManager.UnregisterRunner, performing the runner unregistartion and cleanup, RunnerId={RunnerId}.")]
        public static partial void LogTraceUnregisterRunnerRemove(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERUNREGPREPTASK, Trace, "DefaultRunnerManager, preparing task for disposal of the runner, RunnerId={RunnerId}.")]
        public static partial void LogTracePrepareDisposeTask(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERUNREGDISPASYNC, Trace, "DefaultRunnerManager, run a task returned by DisposeAsync(), RunnerId={RunnerId}.")]
        public static partial void LogTraceRunAsyncDisposeTask(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERUNREGDISPTASK, Trace, "DefaultRunnerManager, run Dispose() in a separate task, RunnerId={RunnerId}.")]
        public static partial void LogTraceRunDisposeTask(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERUNREGNODISP, Trace, "DefaultRunnerManager, no disposal needed, run no task, RunnerId={RunnerId}.")]
        public static partial void LogTraceRunNoDisposeTask(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERRUNNERCLEANUP, Trace, "Starting task observing completion of the runner, RunnerId={RunnerId}.")]
        public static partial void LogTraceRunnerCompleteDispose(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERUNREGEXIT, Trace, "DefaultRunnerManager.UnregisterRunner exited, runners lock released, RunnerId={RunnerId}.")]
        public static partial void LogTraceUnregisterRunnerExit(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERNUMGET, Trace, "Enter DefaultRunnerManager.GetNewRunnerNumber, ActiveSessionId={SessionId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceGetNewRunnerNumber(this ILogger Logger, String SessionId, String TraceIdentifier);

        [LoggerMessage(T_MANAGERNUMGETEXIT, Trace, "Exit DefaultRunnerManager.GetNewRunnerNumber returning the number, ActiveSessionId={SessionId}, RunnerNumber={RunnerNumber}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogTraceGetNewRunnerNumberExit(this ILogger Logger, String SessionId, Int32 RunnerNumber, String TraceIdentifier);

        [LoggerMessage(T_MANAGERNUMRETURN, Trace, "DefaultRunnerManager: Return back unused runner number, ActiveSessionId={SessionId}, RunnerNumber={RunnerNumber}.")]
        public static partial void LogTraceReturnRunnerNumber(this ILogger Logger, String SessionId, Int32 RunnerNumber);

        [LoggerMessage(T_MANAGERINFOACQUIRING, Trace, "DefaultRunnerManager.GetRunnerInfo entered, acquiring runners lock, RunnerId={RunnerId}.")]
        public static partial void LogTraceGetRunnerInfo(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERINFOACQUIRED, Trace, "DefaultRunnerManager.GetRunnerInfo runners lock acquired, RunnerId={RunnerId}.")]
        public static partial void LogTraceGetRunnerInfoLockAcquired(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERINFORELEASED, Trace, "DefaultRunnerManager.GetRunnerInfo exited, runners lock released, RunnerId={RunnerId}, RunnerInfo found: {Found}.")]
        public static partial void LogTraceGetRunnerInfoExit(this ILogger Logger, RunnerId RunnerId, Boolean Found);

        [LoggerMessage(T_MANAGERDISPENDACQUIRING, Trace, "DefaultRunnerManager.FinishDisposalTask entered, acquiring runners lock, RunnerId={RunnerId}.")]
        public static partial void LogTraceFinishDisposalTask(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERDISPENDACQUIRED, Trace, "DefaultRunnerManager.FinishDisposalTask runners lock acquired, RunnerId={RunnerId}.")]
        public static partial void LogTraceFinishDisposalTaskLockAcquired(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERDISPENDRELEASED, Trace, "DefaultRunnerManager.FinishDisposalTask exited, runners lock released, RunnerId={RunnerId}")]
        public static partial void LogTraceFinishDisposalTaskExit(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERABORTALLACQUIRING, Trace, "DefaultRunnerManager.AbortAll entered, acquiring runners lock, ActiveSessionId={SessionId}.")]
        public static partial void LogTraceAbortAll(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERABORTALLACQUIRED, Trace, "DefaultRunnerManager.AbortAll entered, runners lock acquired, ActiveSessionId={SessionId}.")]
        public static partial void LogTraceAbortAllLockAcqired(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERABORTALLABORTING, Trace, "DefaultRunnerManager.AbortAll, aborting a runner, RunnerId={RunnerId}")]
        public static partial void LogTraceAbortAllAbortRunner(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERABORTALLRELEASED, Trace, "DefaultRunnerManager.AbortAll exited, runners lock released, ActiveSessionId={SessionId}.")]
        public static partial void LogTraceAbortAllExit(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPACQUIRING, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync started, runners lock acquired, ActiveSessionId={SessionId}.")]
        public static partial void LogTracePerformRunnersCleanup(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPACQUIRED, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync runners lock acquired, ActiveSessionId={SessionId}.")]
        public static partial void LogTracePerformRunnersCleanupAcquired(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPDUPLICATE, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync duplicate call detected, releasing runners lock and exiting, ActiveSessionId={SessionId}.")]
        public static partial void LogTracePerformRunnersCleanupDuplicate(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPNORUNNERS, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync no runners to clean up , ActiveSessionId={SessionId}.")]
        public static partial void LogTracePerformRunnersCleanupNoRunners(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPRELEASED, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync runners lock released, ActiveSessionId={SessionId}.")]
        public static partial void LogTracePerformRunnersCleanupReleased(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPAWAIT, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync, runners lock released, awaiting task running WaitUnregistratin, ActiveSessionId={SessionId}.")]
        public static partial void LogTracePerformRunnersCleanupAwaiting(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLTASKWAIT, Trace, "DefaultRunnerManager.WaitUnregistration entered and waiting for all runners to unregister, ActiveSessionId={SessionId}.")]
        public static partial void LogTraceCleanupRunnersWaitForUnregistration(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLTASKEXIT, Trace, "DefaultRunnerManager.WaitUnregistration entered, ActiveSessionId={SessionId}.")]
        public static partial void LogTraceCleanupRunnersExit(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPACQUIRING2, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync acquiring 2nd runners lock, ActiveSessionId={SessionId}.")]
        public static partial void LogPerformRunnersCleanupAcquiringLock2(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPACQUIRED2, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync 2nd runners lock acquired, ActiveSessionId={SessionId}.")]
        public static partial void LogTracePerformRunnersCleanupLockAcquired2(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPRELEASED2, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync 2nd runners lock released, returning a task awaiting disposing all runners, ActiveSessionId={SessionId}.")]
        public static partial void LogTracePerformRunnersCleanupLockReleased2(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPDISPOSING, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync disposing this DefaultRunnerManger instance, ActiveSessionId={SessionId}.")]
        public static partial void LogTracePerformRunnersCleanupDisposing(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERCLEANUPENDED, Trace, "DefaultRunnerManager.PerformRunnerCleanupAsync finished, ActiveSessionId={SessionId}.")]
        public static partial void LogTracePerformRunnersCleanupComplete(this ILogger Logger, String SessionId);

        [LoggerMessage(T_MANAGERTRACKCLEANUP, Trace, "DefaultRunnerManager.GetRunnerCleanupTrackingTask entered, acquiring lock, RunnerId={RunnerId}.")]
        public static partial void LogTraceGetRunnerCleanupTracking(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERTRACKCLEANUPLOCKED, Trace, "DefaultRunnerManager.GetRunnerCleanupTrackingTask lock acquired, RunnerId={RunnerId}.")]
        public static partial void LogTraceGetRunnerCleanupTrackingLocked(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERTRACKCLEANUPNEW, Trace, "DefaultRunnerManager.GetRunnerCleanupTrackingTask creating new source for the task, RunnerId={RunnerId}.")]
        public static partial void LogTraceGetRunnerCleanupTrackingNewTaskSource(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(T_MANAGERTRACKCLEANUPEXIT, Trace, "DefaultRunnerManager.GetRunnerCleanupTrackingTask lock released exiting with Found={Found}, RunnerId={RunnerId}.")]
        public static partial void LogTraceGetRunnerCleanupTrackingExit(this ILogger Logger, Boolean Found, RunnerId RunnerId);

#endif

        public static String MakeSessionId(String Id, Int32 Generation)
        {
            StringBuilder sb = new StringBuilder(Id, Id.Length+5);
            sb.Append(':');
            sb.Append(Generation.ToString());
            return sb.ToString();
        }

        public static String MakeSessionId(this IActiveSession ActiveSession)
        {
            return MakeSessionId(ActiveSession.Id, ActiveSession.Generation);
        }

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
