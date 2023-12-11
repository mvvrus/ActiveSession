namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IRunnerManager
    {
        void RegisterSession(IActiveSession SessionKey);
        Int32 GetNewRunnerNumber(IActiveSession SessionKey, String? TraceIdentifier = null);
        void ReturnRunnerNumber(IActiveSession SessionKey, Int32 RunnerNumber);
        void RegisterRunner(IActiveSession SessionKey, int RunnerNumber, IActiveSessionRunner Runner, Type ResultType);
        Task? UnregisterRunner(IActiveSession SessionKey, int RunnerNumber);
        RunnerInfo? GetRunnerInfo(IActiveSession SessionKey, int RunnerNumber);
        Object? RunnerCreationLock { get; }
        void AbortAll(IActiveSession SessionKey);
        public Task PerformRunnersCleanupAsync(IActiveSession SessionKey);
    }
}
