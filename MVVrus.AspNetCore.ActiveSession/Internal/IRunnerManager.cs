namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IRunnerManager
    {
        Int32 GetNewRunnerNumber(String? TraceIdentifier = null);
        void ReturnRunnerNumber(Int32 RunnerNumber);
        void RegisterRunner(int RunnerNumber);
        void UnregisterRunner(int RunnerNumber);
        IServiceProvider Services { get; }
        Object? RunnerCreationLock { get; }
    }
}
