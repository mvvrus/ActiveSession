using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using static MVVrus.AspNetCore.ActiveSession.Internal.RBLogIds;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal static partial class RBLoggingExtensions
    {
        /*
        [LoggerMessage(E_MIDDLEWARE, Error, "The exception occured while creating the ActiveSession middleware.")]
        public static partial void LogErrorMiddlewareCannotBeCreated(this ILogger Logger, Exception AnException);
        [LoggerMessage(T_RUNNERBASE, Trace, "RunnerBase:, RunnerId={RunnerId}")]
        */

        [LoggerMessage(E_RUNNERSTARTBKGFAILED, LogLevel.Error, "An exception occured while starting the runner background execution, RunnerId={RunnerId}.")]
        public static partial void LogErrorStartBkgProcessingFailed(this ILogger Logger, Exception AnException, RunnerId RunnerId);
        [LoggerMessage(E_ENUMERABLERUNNERBASEGETAVAILEXCEPTION, LogLevel.Warning, "An exception occured while calling Enetring EnumerableRunnerBase.GetAvailableMethod, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogErrorEnumerableRunnerBaseGetAvailException(this ILogger Logger, Exception AnException, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(E_ENUMERABLERUNNERBASEGETREQUIREDEXCEPTION, LogLevel.Warning, "An exception occured while calling Enetring EnumerableRunnerBase.GetAvailableMethod, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogErrorEnumerableRunnerBaseGetRequiredException(this ILogger Logger, Exception AnException, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(W_RUNNERBASEUNEXPECTEDSTATUS, LogLevel.Warning, "Unexpected runner status detected while rolling back a start of the runner background execution, RunnerId={RunnerId}, expected: {OldStatus}, detected: {RolledBackStatus}.")]
        public static partial void LogWarningUnexpectedStatusChange(this ILogger Logger, RunnerId RunnerId, RunnerStatus OldStatus, RunnerStatus RolledBackStatus);
        [LoggerMessage(W_ENUMERABLERUNNERBASEPARALLELGET, LogLevel.Warning, "Invalid attempt of getting data in parallel, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogWarningEnumerableRunnerBaseParallelAttempt(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(D_ENUMERABLERUNNERBASERESULT, LogLevel.Debug, "Result to return: (Count:{ResultCount}, Status:{Status}, Position:{Position}) RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}.")]
        public static partial void LogDebugRunnerResult(this ILogger Logger, Exception? AnException, Int32 ResultCount, RunnerStatus Status, Int32 Position, RunnerId RunnerId, String TraceIdentifier);

        [LoggerMessage(T_RUNNERBASECONSENTER, LogLevel.Trace, "RunnerBase: constructor started, RunnerId={RunnerId}")]
        public static partial void LogTraceEnterRunnerBaseConstructor(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_RUNNERBASECONSEXIT, LogLevel.Trace, "RunnerBase: constructor complete, RunnerId={RunnerId}")]
        public static partial void LogTraceEnterRunnerBaseConstructorExit(this ILogger Logger, RunnerId RunnerId);
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

        [LoggerMessage(T_ENUMRUNNERBASEPSEUDOLOCKACQUIRED, LogLevel.Trace, "EnumerableRunnerBase: pseudo-lock acquired, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBasePseudoLockAcquired(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEPSEUDOLOCKRLEASED, LogLevel.Trace, "EnumerableRunnerBase: pseudo-lock released, RunnerId={RunnerId}, TraceIdentifier={TraceIdentifier}")]
        public static partial void LogTraceEnumerableRunnerBasePseudoLockReleased(this ILogger Logger, RunnerId RunnerId, String TraceIdentifier);
        [LoggerMessage(T_ENUMRUNNERBASEDISPOSEASYNC, LogLevel.Trace, "EnumerableRunnerBase: DisposeAsync to be executed, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumerableRunnerBaseDisposeAsyncExecuted(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMRUNNERBASEPREDISPOSE, LogLevel.Trace, "EnumerableRunnerBase: pre-Dispose actions to be executed, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumerableRunnerBasePreDispose(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_ENUMRUNNERBASEDISPOSECORE, LogLevel.Trace, "EnumerableRunnerBase: Dispose actions to be executed, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumerableRunnerBaseDisposeCore(this ILogger Logger, RunnerId RunnerId);
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
        [LoggerMessage(T_ENUMRUNNERBASEABORTCORE, LogLevel.Trace, "EnumerableRunnerBase: Abort-associated actions to be executed, RunnerId={RunnerId}")]
        public static partial void LogTraceEnumerableRunnerBaseAbortCore(this ILogger Logger, RunnerId RunnerId);
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
    }
}
