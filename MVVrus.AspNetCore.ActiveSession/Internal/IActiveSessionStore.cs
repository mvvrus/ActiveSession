namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionStore 
    {
        public IStoreActiveSessionItem? FetchOrCreateSession(ISession Session, String? TraceIdentifier, String? Suffix);
        public void DetachSession(ISession Session, IStoreActiveSessionItem ActiveSessionItem, String? TraceIdentifier);
        public KeyedRunner<TResult> CreateRunner<TRequest, TResult>(ISession Session,
            IStoreActiveSessionItem ActiveSessionItem,
            IRunnerManager RunnerManager,
            TRequest Request, String? TraceIdentifier);
        public IRunner? GetRunner(ISession Session,
            IStoreActiveSessionItem ActiveSessionItem,
            IRunnerManager RunnerManager,
            Int32 RunnerNumber, String? TraceIdentifier);
        public ValueTask<IRunner?> GetRunnerAsync(
            ISession Session,
            IStoreActiveSessionItem ActiveSessionItem,
            IRunnerManager RunnerManager, 
            Int32 RunnerNumber, String? TraceIdentifier, CancellationToken Token
        );
        public Task TerminateSession(ISession Sesion, IStoreActiveSessionItem Session, IRunnerManager RunnerManager, String? TraceIdentifier);
        public IActiveSessionFeature AcquireFeatureObject(ISession? Session, String? TraceIdentier, String? Suffix);
        public void ReleaseFeatureObject(IActiveSessionFeature Feature);
        public ActiveSessionStoreStats? GetCurrentStatistics();
    }
}