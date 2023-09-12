namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IRunnerNumberManager
    {
        Int32 GetNewRunnerNumber(String? TraceIdentifier = null);
        void ReturnRunnerNumber(Int32 RunnerNumber);
        void RegisterRunner(int RunnerNumber);
        void UnregisterRunner(int RunnerNumber);
        IServiceProvider Services { get; }


    }
}
