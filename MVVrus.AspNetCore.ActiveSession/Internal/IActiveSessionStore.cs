namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionStore
    {
        public IActiveSession FetchOrCreateSession(ISession Session, String? TraceIdentifier);
        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(ISession Session, 
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager,
            TRequest Request, String? TraceIdentifier);
        public IActiveSessionRunner<TResult>? GetRunner<TResult>(ISession Session,
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager,
            Int32 RunnerNumber, String? TraceIdentifier);
        public ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            ISession Session,
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager, 
            Int32 RunnerNumber, String? TraceIdentifier, CancellationToken Token
        );
        public Task TerminateSession(IActiveSession Session, IRunnerManager RunnerManager, Boolean Global);
        public IActiveSessionFeature CreateFeatureObject(ISession? Session, String? TraceIdentier);
        public ActiveSessionStoreStats? GetCurrentStatistics();
    }
}