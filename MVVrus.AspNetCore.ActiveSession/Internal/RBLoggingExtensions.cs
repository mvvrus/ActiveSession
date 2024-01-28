using Microsoft.AspNetCore.Http;
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
        public static partial void LogTraceRunnerBaseStateChanged(this ILogger Logger, RunnerId RunnerId, RunnerState State);
        [LoggerMessage(T_RUNNERBASEREACHFINAL, LogLevel.Trace, "RunnerBase: final state reached, signal the runner completion, RunnerId={RunnerId}")]
        public static partial void LogTraceRunnerBaseComeToFinalState(this ILogger Logger, RunnerId RunnerId);
        [LoggerMessage(T_RUNNERBASESTARTED, LogLevel.Trace, "RunnerBase: the runner started, RunnerId={RunnerId}, State={State}")]
        public static partial void LogTraceRunnerBaseStartedInState(this ILogger Logger, RunnerId RunnerId, RunnerState State);

    }
}
