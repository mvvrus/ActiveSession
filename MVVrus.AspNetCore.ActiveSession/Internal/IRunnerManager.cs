﻿namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IRunnerManager
    {
        void RegisterSession(IActiveSession SessionKey);
        Int32 GetNewRunnerNumber(IActiveSession SessionKey, String TraceIdentifier);
        void ReturnRunnerNumber(IActiveSession SessionKey, Int32 RunnerNumber);
        void RegisterRunner(IActiveSession SessionKey, int RunnerNumber, IRunner Runner, Type ResultType, String TraceIdentifier);
        Task? UnregisterRunner(IActiveSession SessionKey, int RunnerNumber);
        RunnerInfo? GetRunnerInfo(IActiveSession SessionKey, int RunnerNumber);
        Object? RunnerCreationLock { get; }
        void AbortAll(IActiveSession SessionKey);
        Task PerformRunnersCleanupAsync(IActiveSession SessionKey);
        Task? GetRunnerCleanupTrackingTask(IActiveSession SessionKey, int RunnerNumber);
    }
}
