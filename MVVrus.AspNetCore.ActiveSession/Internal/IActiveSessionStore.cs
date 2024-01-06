namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionStore
    {
        public IActiveSession? FetchOrCreateSession(ISession Session, String? TraceIdentifier);
        public KeyedRunner<TResult> CreateRunner<TRequest, TResult>(ISession Session, 
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager,
            TRequest Request, String? TraceIdentifier);
        public IRunner<TResult>? GetRunner<TResult>(ISession Session,
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager,
            Int32 RunnerNumber, String? TraceIdentifier);
        public ValueTask<IRunner<TResult>?> GetRunnerAsync<TResult>(
            ISession Session,
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager, 
            Int32 RunnerNumber, String? TraceIdentifier, CancellationToken Token
        );
        public Task TerminateSession(ISession Sesion, IActiveSession Session, IRunnerManager RunnerManager, String? TraceIdentifier);
        public IActiveSessionFeature CreateFeatureObject(ISession? Session, String? TraceIdentier);
        public ActiveSessionStoreStats? GetCurrentStatistics();
    }
}