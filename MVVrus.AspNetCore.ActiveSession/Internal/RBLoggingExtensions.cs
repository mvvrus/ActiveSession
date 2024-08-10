using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using static MVVrus.AspNetCore.ActiveSession.Internal.RBLogIds;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal static partial class RBLoggingExtensions
    {
        [LoggerMessage(E_RUNNERSTARTBKGFAILED, LogLevel.Error, "An exception occured while starting the runner background execution, RunnerId={RunnerId}.")]
        public static partial void LogErrorStartBkgProcessingFailed(this ILogger Logger, Exception AnException, RunnerId RunnerId);
        [LoggerMessage(E_ENUMERABLERUNNERBASEGETAVAILEXCEPTION, LogLevel.Error, "An exception occured while calling EnumerableRunnerBase.GetAvailable method, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogErrorEnumerableRunnerBaseGetAvailException(this ILogger Logger, Exception AnException, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(E_ENUMERABLERUNNERBASEGETREQUIREDEXCEPTION, LogLevel.Error, "An exception occured while calling EnumerableRunnerBase.GetRequiredAsync method, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogErrorEnumerableRunnerBaseGetRequiredException(this ILogger Logger, Exception AnException, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(E_ENUMADAPTERRUNNERDISPOSEEXCEPTION, LogLevel.Error, "An exception occured during disposing EnumAdapterRunner, RunnerId={RunnerId}.")]
        public static partial void LogErrorEnumAdapterRunnerDisposeException(this ILogger Logger, Exception AnException, RunnerId RunnerId);
        [LoggerMessage(E_ENUMADAPTERRUNNERSOURCEENUMERATIONEXCEPTION, LogLevel.Error, "An exception occured during the source enumeration in EnumAdapterRunner, RunnerId={RunnerId}.")]
        public static partial void LogErrorEnumAdapterRunnerSourceEnumerationException(this ILogger Logger, Exception AnException, RunnerId RunnerId);
        [LoggerMessage(E_ENUMADAPTERRUNNERAWAITCONTINUATIONEXCEPTION, LogLevel.Error, "An exception occured during scheduling a continuation in EnumAdapterRunner, RunnerId={RunnerId}.")]
        public static partial void LogErrorEnumAdapterRunnerContinuationException(this ILogger Logger, Exception AnException, RunnerId RunnerId);
        [LoggerMessage(E_ASYNCENUMADAPTERRUNNERSOURCEENUMERATIONEXCEPTION, LogLevel.Error, "An exception occured during the source enumeration in AsyncEnumAdapterRunner, RunnerId={RunnerId}.")]
        public static partial void LogErrorAsyncEnumAdapterRunnerSourceEnumerationException(this ILogger Logger, Exception? AnException, RunnerId RunnerId);
        [LoggerMessage(E_SESSIONPROCESSRUNNERCONTINTERNALERROR, LogLevel.Error, "SessionProcessRunner: internal error, continuation of a task with invalid status, Status={Status}, RunnerId={RunnerId}.")]
        public static partial void LogErrorSessionProgressBkgEndedInternal(this ILogger Logger, TaskStatus Status, RunnerId RunnerId);

        [LoggerMessage(W_RUNNERBASEUNEXPECTEDSTATUS, LogLevel.Warning, "Unexpected runner status detected while rolling back a start of the runner background execution, RunnerId={RunnerId}, expected: {OldStatus}, detected: {RolledBackStatus}.")]
        public static partial void LogWarningUnexpectedStatusChange(this ILogger Logger, RunnerId RunnerId, RunnerStatus OldStatus, RunnerStatus RolledBackStatus);
        [LoggerMessage(W_ENUMERABLERUNNERBASEPARALLELGET, LogLevel.Warning, "Invalid attempt of getting data in parallel, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogWarningEnumerableRunnerBaseParallelAttempt(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(W_ENUMERABLERUNNERBASEBADPARAM, LogLevel.Warning, "Invalid parameter value, MethodName={MethodName}, ParamName={ParamName}, Value={ParamValue}, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogWarningBadParam(this ILogger Logger, String MethodName, String ParamName, Int32 ParamValue, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(W_SESSIONPROCESSRUNNERBADPARAM, LogLevel.Warning, "SessionProcessRunner: bad parameter, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogWarningSessionProcessBadParameters(this ILogger Logger, Exception? AnException, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(W_SESSIONPROCESSRUNNERTASKRESULTALREADYSET, LogLevel.Warning, "SessionProcessRunner: pending task result is already set, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogWarningTaskOutcomeAlreadySet(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(I_RUNNERBASESTARTING, LogLevel.Information, "The runner to be started, RunnerId={RunnerId}.")]
        public static partial void LogInfoRunnerStarting(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(I_RUNNERBASESTARTFAILED, LogLevel.Information, "An attempt to start the runer failed, RunnerId={RunnerId}.")]
        public static partial void LogInfoRunnerStartFailed(this ILogger Logger, Exception Exception, RunnerId RunnerId);
        [LoggerMessage(I_RUNNERBASECOMPLETED, LogLevel.Information, "The runner came to its final state, RunnerId={RunnerId}, Status={FinalStatus}.")]
        public static partial void LogInfoRunnerCompleted(this ILogger Logger, RunnerId RunnerId, RunnerStatus FinalStatus);
        [LoggerMessage(I_RUNNERBASEBKGSTARTED, LogLevel.Information, "The runner background processing started, RunnerId={RunnerId}.")]
        public static partial void LogInfoStartBackground(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(I_RUNNERBASEBKGFINISHED, LogLevel.Information, "The runner background processing ended, RunnerId={RunnerId}.")]
        public static partial void LogInfoFinishBackground(this ILogger Logger, RunnerId RunnerId);

        [LoggerMessage(D_ENUMERABLERUNNERBASERESULT, LogLevel.Debug, "Result to return: (Count:{ResultCount}, Status:{Status}, Position:{Position}) RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogDebugEnumerableRunnerResult(this ILogger Logger, Exception? AnException, Int32 ResultCount, RunnerStatus Status, Int32 Position, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(D_ENUMADAPTERRUNNERPARAMS, LogLevel.Debug, 
            "New EnumAdapterRunner created, RunnerId={RunnerId} parameters:(" +
            "PassSourceOnership={PassSourceOnership}, CompletionTokenSourcePresent={CompletionTokenSourcePresent}, " +
            "PassCtsOwnership={PassCtsOwnership}, EffectiveDefaultAdvance={EffectiveDefaultAdvance}, " +
            "EffectiveEnumAheadLimit={EffectiveEnumAheadLimit}, StartInConstructor={StartInConstructor} )"
            )]
        public static partial void LogDebugEnumAdapterRunnerConstructor(this ILogger Logger,
                    RunnerId RunnerId,
                    Boolean PassSourceOnership,
                    Boolean  CompletionTokenSourcePresent,
                    Boolean PassCtsOwnership,
                    Int32 EffectiveDefaultAdvance,
                    Int32 EffectiveEnumAheadLimit,
                    Boolean StartInConstructor);
        [LoggerMessage(D_ASYNCENUMADAPTERRUNNERPARAMS, LogLevel.Debug,
            "New EnumAdapterRunner created, RunnerId={RunnerId} parameters:(" +
            "PassSourceOnership={PassSourceOnership}, CompletionTokenSourcePresent={CompletionTokenSourcePresent}, " +
            "PassCtsOwnership={PassCtsOwnership}, EffectiveDefaultAdvance={EffectiveDefaultAdvance}, " +
            "EffectiveEnumAheadLimit={EffectiveEnumAheadLimit}, StartInConstructor={StartInConstructor} )"
            )]
        public static partial void LogDebugAsyncEnumAdapterRunnerConstructor(this ILogger Logger,
                    RunnerId RunnerId,
                    Boolean PassSourceOnership,
                    Boolean CompletionTokenSourcePresent,
                    Boolean PassCtsOwnership,
                    Int32 EffectiveDefaultAdvance,
                    Int32 EffectiveEnumAheadLimit,
                    Boolean StartInConstructor);
        [LoggerMessage(D_TIMESERIESRUNNERPARAMS, LogLevel.Debug,
            "New TimeSeriesRunner created, RunnerId={RunnerId} parameters:(" +
            "Interval={Interval}, Count={Count})" 
            )]
        public static partial void LogDebugTimeSeriesRunnerConstructor(this ILogger Logger,
                    RunnerId RunnerId,
                    TimeSpan Interval, 
                    Int32? Count);
        [LoggerMessage(D_SESSIONPROCESSRUNNERPARAMS, LogLevel.Debug,
            "New SessionProcessRunner created, RunnerId={RunnerId} parameters:(" +
            "CompletionTokenSourcePresent={CompletionTokenSourcePresent}" +
            ", PassCtsOwnership={PassCtsOwnership}" +
            ", SyncBkg={SyncBkg}" +
            ", BkgReturnsResult={BkgReturnsResult}" +
            ")"
            )]
        public static partial void LogDebugSessionRunnerConstructor(this ILogger Logger,
                    RunnerId RunnerId,
                    Boolean CompletionTokenSourcePresent,
                    Boolean PassCtsOwnership,
                    Boolean SyncBkg,
                    Boolean BkgReturnsResult);
        [LoggerMessage(D_SESSIONPROCESSRUNNERRESULT, LogLevel.Debug, "Result to return: (Result:{ResultString}, Status:{Status}, Position:{Position}) RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogDebugSessionProcessRunnerResult(this ILogger Logger, Exception? AnException, String ResultString, RunnerStatus Status, Int32 Position, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_RUNNERBASECONSENTER, LogLevel.Trace, "RunnerBase: constructor started, RunnerId={RunnerId}")]
        public static partial void LogTraceRunnerBaseConstructorEnter(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_RUNNERBASECONSEXIT, LogLevel.Trace, "RunnerBase: constructor complete, RunnerId={RunnerId}")]
        public static partial void LogTraceRunnerBaseConstructorExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_RUNNERBASEDISPOSE, LogLevel.Trace, "RunnerBase: Dispose() called, RunnerId={RunnerId}")]
        public static partial void LogTraceRunnerBaseDisposing(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_RUNNERBASESESTATENOTSTARTED, LogLevel.Trace, "RunnerBase: attempt to return State to NotStarted ignored, RunnerId={RunnerId}")]
        public static partial void LogTraceRunnerBaseReturnToNotStartedStateAttempt(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_RUNNERBASECHANGEFINALSTATE, LogLevel.Trace, "RunnerBase: attempt change final state ignored, RunnerId={RunnerId}")]
        public static partial void LogTraceRunnerBaseChangeFinalStateAttempt(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_RUNNERBASESTATESET, LogLevel.Trace, "RunnerBase: new state set, RunnerId={RunnerId}, State={State}")]
        public static partial void LogTraceRunnerBaseStateChanged(this ILogger Logger, RunnerId RunnerId, RunnerStatus State);
        [LoggerMessage(T_RUNNERBASEREACHFINAL, LogLevel.Trace, "RunnerBase: final state reached, signal the runner completion, RunnerId={RunnerId}")]
        public static partial void LogTraceRunnerBaseComeToFinalState(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_RUNNERBASESTARTED, LogLevel.Trace, "RunnerBase: the runner started, RunnerId={RunnerId}, State={State}")]
        public static partial void LogTraceRunnerBaseStartedInState(this ILogger Logger, RunnerId RunnerId, RunnerStatus State);
        [LoggerMessage(T_RUNNERBASESABORTCALLED, LogLevel.Trace, "RunnerBase.Abort() is called, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier} ")]
        public static partial void LogTraceRunnerBaseAbortCalled(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_ENUMRUNNERBASECONSENTER, LogLevel.Trace, "EnumerableRunnerBase: constructor started, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumerableRunnerBaseConstructorEnter(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMRUNNERBASECONSEXIT, LogLevel.Trace, "EnumerableRunnerBase: constructor complete, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumerableRunnerBaseConstructorExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMRUNNERBASEDISPOSEASYNC, LogLevel.Trace, "EnumerableRunnerBase: DisposeAsync to be executed, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumerableRunnerBaseDisposeAsyncExecuted(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMRUNNERBASEPREDISPOSE, LogLevel.Trace, "EnumerableRunnerBase: pre-Dispose actions to be executed, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumerableRunnerBasePreDispose(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMRUNNERBASEDISPOSECORE, LogLevel.Trace, "EnumerableRunnerBase: Dispose actions to be executed, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumerableRunnerBaseDisposeCore(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMRUNNERBASEPSEUDOLOCKACQUIRED, LogLevel.Trace, "EnumerableRunnerBase: pseudo-lock acquired, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBasePseudoLockAcquired(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEPSEUDOLOCKRLEASED, LogLevel.Trace, "EnumerableRunnerBase: pseudo-lock released, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBasePseudoLockReleased(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEABORTCORE, LogLevel.Trace, "EnumerableRunnerBase: Abort-associated actions to be executed, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAbortCore(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEQUEUEADDITIONCANCELED, LogLevel.Trace, "EnumerableRunnerBase: blocked addition to the background queue is canceled, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumerableRunnerQueueAdditionCanceled(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMRUNNERBASEGETAVAILABLE, LogLevel.Trace, "Enetring EnumerableRunnerBase.GetAvailable, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseGetAvailable(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEGETAVAILABLEEXIT, LogLevel.Trace, "Exiting EnumerableRunnerBase.GetAvailable, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseGetAvailableExit(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEGETREQUIRED, LogLevel.Trace, "Enetring EnumerableRunnerBase.GetRequiredAsync, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseGetRequired(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEGETREQUIREDSTARTUPCOMPLETE, LogLevel.Trace, "EnumerableRunnerBase.GetRequiredAsync: background process has been started up , RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseGetRequiredStartupComplete(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEGETREQUIREDTRYSYNCPATH, LogLevel.Trace, "EnumerableRunnerBase.GetRequiredAsync: trying to satisfy request by already present data, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseGetRequiredTrySyncPath(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEGETREQUIREDSYNCEXIT, LogLevel.Trace, "EnumerableRunnerBase.GetRequiredAsync returned synchronously with already completed task, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseGetRequiredSyncExit(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEGETREQUIREDFETCHTASK, LogLevel.Trace, "EnumerableRunnerBase.GetRequiredAsync: forming task to fetch more data in background, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseGetRequiredFormFetchTask(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEGETREQUIREDSTARTUPANDFETCHTASK, LogLevel.Trace, "EnumerableRunnerBase.GetRequiredAsync: forming task to complete startup and fetch data in background, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseGetRequiredFormStartupAndfetchTask(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEGETREQUIREDRETURNASYNC, LogLevel.Trace, "EnumerableRunnerBase.GetRequiredAsync returned task to complete the operation asynchronously , RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseGetRequiredExitAsync(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEFETCH, LogLevel.Trace, "EnumerableRunnerBase.FetchAvailable entered, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseFetchAvailable(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEFETCHFINAL, LogLevel.Trace, "EnumerableRunnerBase.FetchAvailable final stage detected, nothing to fetch any more, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseFetchAvailableFinal(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEFETCHSTASHEDALL, LogLevel.Trace, "EnumerableRunnerBase.FetchAvailable fetch all stashed data, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseFetchAvailableStashedAll(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEFETCHSTASHEDPART, LogLevel.Trace, "EnumerableRunnerBase.FetchAvailable fetch part of stashed data, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseFetchAvailableStashedPartial(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEFETCHQUEUE, LogLevel.Trace, "EnumerableRunnerBase.FetchAvailable fetch some data form queue, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseFetchAvailableFromQueue(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEFETCHEXIT, LogLevel.Trace, "EnumerableRunnerBase.FetchAvailable is completed, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}, result:{Result}")]
        public static partial void LogTraceEnumerableRunnerBaseFetchAvailableExit(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier, Boolean Result);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCSTARTBKGSUCCESS, LogLevel.Trace, "EnumerableRunnerBase continuation: background execution has been started OK, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncStartBkgSuccess(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCSTARTBKGENOUGHDATA, LogLevel.Trace, "EnumerableRunnerBase continuation: enough data has been fetched already in background to return them synchrnously. , RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncEnoughDataOnStartBkg(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCSTARTBKGINSUFFDATA, LogLevel.Trace, "EnumerableRunnerBase continuation: not enough data has been fetched in background yet, need to fetch more asynchronously, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncInsuffDataOnStartBkg(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCATTACHFETCHCONT, LogLevel.Trace, "EnumerableRunnerBase tasks: result extracting tasks are attached to the fetch task, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncFetchContinuations(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCFETCHFAILED, LogLevel.Trace, "EnumerableRunnerBase continuation: fetch task execution failed, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncFetchFailed(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCSETFAILRESULT, LogLevel.Trace, "EnumerableRunnerBase continuation: previous task threw an exception, pass it as a GetRequiredAsync result, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncSetFailResult(this ILogger Logger, Exception? AnException, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCFAILRESULTSET, LogLevel.Trace, "EnumerableRunnerBase continuation: exception as a result of GetRequiredAsync is passed, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncFailResultSet(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCFETCHCANCELED, LogLevel.Trace, "EnumerableRunnerBase continuation: fetch task has been canceled, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncFetchCanceled(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCSETCANCELRESULT, LogLevel.Trace, "EnumerableRunnerBase continuation: previous task has been canceled, pass cancelation status as a GetRequiredAsync result, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncSetCancelResult(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCCANCELRESULTSET, LogLevel.Trace, "EnumerableRunnerBase continuation: cancellation status as a result of GetRequiredAsync is passed, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncCancelResultSet(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCFETCHSUCCESS, LogLevel.Trace, "EnumerableRunnerBase continuation: async fetch was successful, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncFetchCompletedSuccess(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCSUCCESSRESULTSET, LogLevel.Trace, "EnumerableRunnerBase continuation: result of successful completion of GetRequiredAsync is passed, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncSuccessResultSet(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEASYNCSTASHORPHANNED, LogLevel.Trace, "EnumerableRunnerBase async processing: data fetched in unsuccessful fetch have been stashed, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncStashOrphanedFetched(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASERESULTMAKEFORASYNC, LogLevel.Trace, "EnumerableRunnerBase result: prepare the result for a synchronous return , RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncMakeResult(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASERESULTSETFINALSTATUS, LogLevel.Trace, "EnumerableRunnerBase result: final status for the runner set, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseAsyncSetFinalStatus(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASERESULTTOMAKE, LogLevel.Trace, "EnumerableRunnerBase result: make the result for a return, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBaseMakeSyncResult(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_ENUMADAPTERRUNNERCONSENTER, LogLevel.Trace, "EnumAdapterRunner: constructor started, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterConstructorEnter(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERCONSEXIT, LogLevel.Trace, "EnumAdapterRunner: constructor complete, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterConstructorExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERDISPOSECORE, LogLevel.Trace, "EnumAdapterRunner: Dispose actions to be executed, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerDisposeCore(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERPREDISPOSE, LogLevel.Trace, "EnumAdapterRunner: Starting pre-dispose actions, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerPreDispose(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERPREDISPOSEEXIT, LogLevel.Trace, "EnumAdapterRunner: Ending pre-dispose actions, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerPreDisposeExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERRELEASESOURCE, LogLevel.Trace, "EnumAdapterRunner: Releasing source enumerable, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerReleaseSource(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERSOURCEDISPOSED, LogLevel.Trace, "EnumAdapterRunner: Source enumerable disposed, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerSourceDisposed(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERSTARTBKGENTER, LogLevel.Trace, "EnumAdapterRunner: StartBackgroundExecution entered, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerStartBackgroundEnter(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERSTARTBKGEXIT, LogLevel.Trace, "EnumAdapterRunner: StartBackgroundExecution exited, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerStartBackgroundExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERENUMSRCSTART, LogLevel.Trace, "EnumAdapterRunner.EnumerateSource: Start an enumeration of the source, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerEnumerateSourceStart(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERENUMSRCNEWITERATION, LogLevel.Trace, "EnumAdapterRunner.EnumerateSource: A new item acquired in the enumeration loop, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerEnumerateSourceNewIteration(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERENUMSRCLOOPBREAK, LogLevel.Trace, "EnumAdapterRunner.EnumerateSource: Break the enumeration loop, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerEnumerateSourceIterationBreak(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERENUMSRCITEMADDED, LogLevel.Trace, "EnumAdapterRunner.EnumerateSource: The item is added to the queue, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerEnumerateSourceItemAdded(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERENUMSRCCANCELAFTERADD, LogLevel.Trace, "EnumAdapterRunner.EnumerateSource: Cancellation after the addition, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerEnumerateSourceCanceledAfterIteration(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERENUMSRCLOOPENDED, LogLevel.Trace, "EnumAdapterRunner.EnumerateSource: The enumeration loop ended, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerEnumerateSourceIterationExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERENUMSRCFINALIZE, LogLevel.Trace, "EnumAdapterRunner.EnumerateSource: Performing final operations, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerEnumerateSourceFinalize(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERENUMSRCEXIT, LogLevel.Trace, "EnumAdapterRunner.EnumerateSource: The enumeration task is done, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerEnumerateSourceExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERAWAITSCHEDULE, LogLevel.Trace, "EnumAdapterRunner awaiter: schedule a continuation, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerEnumerateScheduleContinuation(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERAWAITSCHEDULELASTCHANCEQUEUE, LogLevel.Trace, "EnumAdapterRunner awaiter: run scheduled continuation because it may be the last chance, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerSheduleQueueContnuationToRunAsLastChancePossible(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERAWAITSCHEDULEEXIT, LogLevel.Trace, "EnumAdapterRunner awaiter: scheduling the continuation done, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerEnumerateScheduleContinuationDone(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERAWAITQUEUE, LogLevel.Trace, "EnumAdapterRunner awaiter: queue a previosly scheduled continuation (if any) to run, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerQueueContnuationToRun(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERAWAITQUEUEREALLY, LogLevel.Trace, "EnumAdapterRunner awaiter: queue a previosly scheduled continuation (if any) to run, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerQueueContnuationToRunReally(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERAWAITQUEUEEXIT, LogLevel.Trace, "EnumAdapterRunner awaiter: queueing a possible continuation to run finished, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumAdapterRunnerQueueContnuationToRunExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMADAPTERRUNNERFETCHREQUIREDENTER, LogLevel.Trace, "EnumAdapterRunner.FetchRequiredAsync entered, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumAdapterRunnerFetchRequiredAsyncEnter(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMADAPTERRUNNERFETCHREQUIREDLOOPSTART, LogLevel.Trace, "EnumAdapterRunner.FetchRequiredAsync: an item extraction loop started, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumAdapterRunnerFetchRequiredAsyncLoopStart(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMADAPTERRUNNERFETCHREQUIREDLOOPNEXT, LogLevel.Trace, "EnumAdapterRunner.FetchRequiredAsync: the next loop iteration, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumAdapterRunnerFetchRequiredAsyncLoopNext(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMADAPTERRUNNERFETCHREQUIREDITEMTAKEN, LogLevel.Trace, "EnumAdapterRunner.FetchRequiredAsync: a new item is taken from the queue, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumAdapterRunnerFetchRequiredAsyncItemTaken(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMADAPTERRUNNERFETCHREQUIREDNOMOREITEMS, LogLevel.Trace, "EnumAdapterRunner.FetchRequiredAsync: no more items to extract, break the loop, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumAdapterRunnerFetchRequiredAsyncNoMoreItems(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMADAPTERRUNNERFETCHREQUIREDBEFOREAWAITING, LogLevel.Trace, "EnumAdapterRunner.FetchRequiredAsync: awaiting background enumeration , RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumAdapterRunnerFetchRequiredAsyncBeforeAwaiting(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMADAPTERRUNNERFETCHREQUIREDAFTERAWAITING, LogLevel.Trace, "EnumAdapterRunner.FetchRequiredAsync: continue the loop after awaiting , RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumAdapterRunnerFetchRequiredAsyncAfterAwaiting(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMADAPTERRUNNERFETCHREQUIREDEXIT, LogLevel.Trace, "EnumAdapterRunner.FetchRequiredAsync exited, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumAdapterRunnerFetchRequiredAsyncExit(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMADAPTERRUNNERDOABORT, LogLevel.Trace, "EnumAdapterRunner: Abort-associated actions to be executed, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumAdapterRunnerDoAbort(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERCONSENTER, LogLevel.Trace, "AsyncEnumAdapterRunner: constructor started, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterConstructorEnter(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERCONSEXIT, LogLevel.Trace, "AsyncEnumAdapterRunner: constructor complete, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterConstructorExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERDISPOSECORE, LogLevel.Trace, "AsyncEnumAdapterRunner: Dispose actions to be executed, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerDisposeCore(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERPREDISPOSE, LogLevel.Trace, "AsyncEnumAdapterRunner: Starting pre-dispose actions, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterPreDispose(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERPREDISPOSEEXIT, LogLevel.Trace, "AsyncEnumAdapterRunner: Ending pre-dispose actions, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerPreDisposeExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERSOURCEDISPOSED, LogLevel.Trace, "AsyncEnumAdapterRunner: Source enumerable disposed, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerSourceDisposed(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERSTARTBKGENTER, LogLevel.Trace, "AsyncEnumAdapterRunner: StartBackgroundExecution entered, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerStartBackgroundEnter(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERSTARTBKGEXIT, LogLevel.Trace, "AsyncEnumAdapterRunner: StartBackgroundExecution exited, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerStartBackgroundExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERENUMSRCSTEPCOMPLETE, LogLevel.Trace, "AsyncEnumAdapterRunner source enumeration: A task for the next enumeration step in the chain complete, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerEnumerateSourceStepComplete(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERENUMSRCSTEPCANCELED, LogLevel.Trace, "AsyncEnumAdapterRunner source enumeration: a step was canceled, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerEnumerateSourceStepCanceled(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERENUMSRCCHAINBREAK, LogLevel.Trace, "AsyncEnumAdapterRunner source enumeration: Break the enumeration chain, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerEnumerateSourceChainBreak(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERENUMSRCITEMADDED, LogLevel.Trace, "AsyncEnumAdapterRunner source enumeration: The item is added to the queue, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerEnumerateSourceItemAdded(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERENUMSRCMOVENEXT, LogLevel.Trace, "AsyncEnumAdapterRunner source enumeration: start the next step of the enumeration chain, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerEnumerateSourceIterationContnue(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERENUMSRCDONE, LogLevel.Trace, "AsyncEnumAdapterRunner source enumeration: enumeration chain done, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerEnumerateSourceIterationDone(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERENUMSRCSTEPENDED, LogLevel.Trace, "AsyncEnumAdapterRunner source enumeration: This step of the enumeration chain complete, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerEnumerateSourceIterationExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERTRYRELESECONTEXT, LogLevel.Trace, "AsyncEnumAdapterRunner: Try to release a current fetch context, RunnerId={RunnerId}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerTryReleaseFetchContext(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERFETCHPENDING, LogLevel.Trace, "AsyncEnumAdapterRunner source enumeration: a fetch task is pending, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerEnumerateSourceFetchTaskActive(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERFETCHCANCELED, LogLevel.Trace, "AsyncEnumAdapterRunner source enumeration: canceling the fetch task, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerEnumerateSourceCancelFetchTask(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERFETCHCOPIED, LogLevel.Trace, "AsyncEnumAdapterRunner source enumeration: copy items from the queue to a fetch result, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerEnumerateSourceCopyFetchedItems(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERFETCHCOMPLETE, LogLevel.Trace, "AsyncEnumAdapterRunner source enumeration: completing the fetch task successfully, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerEnumerateSourceCompleteFetchTask(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERFETCHCONTEXTRELESED, LogLevel.Trace, "AsyncEnumAdapterRunner: The current fetch context released, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerFetchContextReleased(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERFETCHENTER, LogLevel.Trace, "AsyncEnumAdapterRunner.FetchRequiredAsync entered, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerFetchRequiredAsyncEnter(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERFETCHFAILASDISPOSED, LogLevel.Trace, "EnumAdapterRunner: make fetch task failed because of disposing, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerFetchRequiredAsyncFailAsDisposed(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERFETCHSONTEXTSTORED, LogLevel.Trace, "AsyncEnumAdapterRunner: fetch context is stored for future, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerFetchRequiredAsyncStoreContext(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ASYNCENUMADAPTERRUNNERFETCHEXIT, LogLevel.Trace, "AsyncEnumAdapterRunner.FetchRequiredAsync exited, the task returned, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceAsyncEnumAdapterRunnerFetchRequiredAsyncExit(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_SESSIONPROCESSSTARTBKGENTER, LogLevel.Trace, "SessionProgressRunner.StartBackgroundRunner entered, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessStartBackgroundExecution(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSSTARTBKGTASK, LogLevel.Trace, "SessionProgressRunner: Background task started, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessStartBackgroundTask(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSSTARTBKGEXIT, LogLevel.Trace, "SessionProgressRunner.StartBackgroundRunner exited, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessStartBackgroundExecutionExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSPREDISPOSE, LogLevel.Trace, "SessionProgressRunner: aborting baclground task, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessAbortBkgTask(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSDODISPOSE, LogLevel.Trace, "SessionProgressRunner: perform disposing, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessDisposing(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSBKGTASKAWAITED, LogLevel.Trace, "SessionProgressRunner: background task terminated, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessBkgTaskAwaited(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSENTERDISPOSEASYNC, LogLevel.Trace, "SessionProgressRunner.DisposeAsync started to run, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessDisposeAsync(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSGETAVAILENTER, LogLevel.Trace, "SessionProcessRunner.GetAvailable entered, acquiring the lock, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetAvailableEntered(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETAVAILLOCKACQUIRED, LogLevel.Trace, "SessionProcessRunner.GetAvailable: the lock acquired, checking and ajusting parameters, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetAvailableLockAckuired(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETAVAILALL, LogLevel.Trace, "SessionProcessRunner.GetAvailable: the current point of a background execution is reached, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetAvailableAll(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETAVAILNOTALL, LogLevel.Trace, "SessionProcessRunner.GetAvailable: the current point of a background execution is not reached, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetAvailableNotAll(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETAVAILTRYSETSTATUS, LogLevel.Trace, "SessionProcessRunner.GetAvailable: trying to change the runner Status, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetAvailableTrySetNewStatus(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETAVAILSTATUSSET, LogLevel.Trace, "SessionProcessRunner.GetAvailable: the runner status have been just changed, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetAvailableNewStatusSet(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETAVAILLOCKRELEASED, LogLevel.Trace, "SessionProcessRunner.GetAvailable: the lock released, exiting, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetAvailableLockReleased(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETREQASYNCENTER, LogLevel.Trace, "SessionProcessRunner.GetRequiredAsync entered, acquiring the lock, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetRequiredAsyncEntered(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETREQASYNCLOCKACQUIRED, LogLevel.Trace, "SessionProcessRunner.GetRequiredAsync: the lock acquired, checking and ajusting parameters, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetRequiredAsyncLockAckuired(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETREQASYNCSYNCPATH, LogLevel.Trace, "SessionProcessRunner.GetRequiredAsync: the method can be executed synchronously, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetRequiredAsyncSynchronous(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETREQASYNCNOTALL, LogLevel.Trace, "SessionProcessRunner.GetRequiredAsync: the current point of a background execution is not reached, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetRequiredAsyncNotAll(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETREQASYNCALL, LogLevel.Trace, "SessionProcessRunner.GetRequiredAsync: the current point of a background execution is reached, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetRequiredAsyncAll(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETREQASYNCTRYSETSTATUS, LogLevel.Trace, "SessionProcessRunner.GetRequiredAsync: trying to change the runner Status, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetRequiredAsyncTrySetNewStatus(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETREQASYNCSTATUSSET, LogLevel.Trace, "SessionProcessRunner.GetRequiredAsync: the runner status have been just changed, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetRequiredAsyncNewStatusSet(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETREQASYNCASYNCPATH, LogLevel.Trace, "SessionProcessRunner.GetRequiredAsync: synchronous execution is not possible, schedule a continuation task, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetRequiredAsyncAsynchronous(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETREQASYNCTASENQUEUED, LogLevel.Trace, "SessionProcessRunner.GetRequiredAsync: the continuation task is enqued to the completion queue, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetRequiredAsyncTaskEnqueued(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSGETREQASYNCLOCKRELEASED, LogLevel.Trace, "SessionProcessRunner.GetRequiredAsync: the lock released, exiting, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessGetRequiredAsyncLockReleased(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSPARAMSENTER, LogLevel.Trace, "SessionProcessRunner.CheckAndNormalizeParams entered, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessCheckAndNormalizeParams(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSPARAMSADJUSTDEFAULT, LogLevel.Trace, "SessionProcessRunner.CheckAndNormalizeParams: defaults adjusted, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessCheckAndNormalizeParamsDefaultAdjusted(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSPARAMSEXIT, LogLevel.Trace, "SessionProcessRunner.CheckAndNormalizeParams exited, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessCheckAndNormalizeParamsExit(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSBKGENDENTER, LogLevel.Trace, "SessionProcessRunner background task continuation: entered, acquiring the lock, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessBkgEnded(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSBKGENDLOCKACQUIRED, LogLevel.Trace, "SessionProcessRunner background task continuation: the lock acquired, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessBkgEndedLockAcquired(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSBKGENDRANTOCOMPLETION, LogLevel.Trace, "SessionProcessRunner background task continuation: the task was completed successfully, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessBkgEndedRanToCompletion(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSBKGENDACCEPTRESULT, LogLevel.Trace, "SessionProcessRunner background task continuation: accept the final result of the task, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessBkgEndedAcceptResult(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSBKGENDFAULTED, LogLevel.Trace, "SessionProcessRunner background task continuation: the task was canceled, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessBkgEndedCanceled(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSBKGENDCANCELED, LogLevel.Trace, "SessionProcessRunner background task continuation: the task throwed an exception, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessBkgEndedFaulted(this ILogger Logger, Exception? AnException, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSBKGENDPEDINGLOOP, LogLevel.Trace, "SessionProcessRunner background task continuation: process pending tasks from GetRequiredAsync, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessBkgEndedCompletePendingTasks(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSBKGENDPROCESSAPENDING, LogLevel.Trace, "SessionProcessRunner background task continuation: processing a task, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessBkgEndedCompleteAPendingTask(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSBKGENDEXIT, LogLevel.Trace, "SessionProcessRunner background task continuation: the lock released, exiting, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessBkgEndedExit(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSPENDINGSETCANCELED, LogLevel.Trace, "SessionProcessRunner pending task processing: cancel the task, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessPendingTaskSetCanceled(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSPENDINGSETEXCEPTION, LogLevel.Trace, "SessionProcessRunner pending task processing: fail the task, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessPendingTaskSetException(this ILogger Logger, Exception? AnException, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSPENDINGSETRESULT, LogLevel.Trace, "SessionProcessRunner pending task processing: set the task result, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessPendingTaskSetResult(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSPENDINGALREADYCANCELED, LogLevel.Trace, "SessionProcessRunner pending task processing: the task has been already canceled, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessPendingTaskAlreadyCanceled(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSCALLBACKENTER, LogLevel.Trace, "SessionProcessRunner callback entered, acquiring a  lock, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessCallback(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSCALLBACKCANCELED, LogLevel.Trace, "SessionProcessRunner callback: signal that the runner was aborted , RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessCallbackCanceled(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSCALLBACKLOCKACQUIRED, LogLevel.Trace, "SessionProcessRunner callback: the lock acquired, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessCallbackLockAcquired(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSCALLBACKPENDINGLOOP, LogLevel.Trace, "SessionProcessRunner callback: process pending tasks from GetRequiredAsync, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessCallbackCompletePendingTasks(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_SESSIONPROCESSCALLBACKPROCESSAPENDING, LogLevel.Trace, "SessionProcessRunner callback: processing a task, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceSessionProcessCallbackCompleteAPendingTask(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_SESSIONPROCESSCALLBACKEXIT, LogLevel.Trace, "SessionProcessRunner callback: the lock released, exiting, RunnerId={RunnerId}")]
        public static partial void LogTraceSessionProcessCallbackExit(this ILogger Logger, RunnerId RunnerId);

    }
}
