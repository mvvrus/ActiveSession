namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IRunnerManager
    {
        void RegisterSession(IActiveSession SessionKey);
        Int32 GetNewRunnerNumber(IActiveSession SessionKey, String? TraceIdentifier = null);
        void ReturnRunnerNumber(IActiveSession SessionKey, Int32 RunnerNumber);
        void RegisterRunner(IActiveSession SessionKey, int RunnerNumber, IActiveSessionRunner Runner, Type ResultType);
        void UnregisterRunner(IActiveSession SessionKey, int RunnerNumber);
        Object? RunnerCreationLock { get; }
        Boolean WaitForRunners(IActiveSession SessionKey, Int32 Timeout);
    }
}
