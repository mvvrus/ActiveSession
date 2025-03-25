namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionStore 
    {
        public IStoreActiveSessionItem? FetchOrCreateSession(ISession Session, String? TraceIdentifier, String? Suffix);
        public void DetachSession(ISession Session, IStoreActiveSessionItem ActiveSessionItem, String? TraceIdentifier);
        public KeyedRunner<TResult> CreateRunner<TRequest, TResult>(ISession Session,
            IStoreActiveSessionItem ActiveSessionItem,
            TRequest Request, String? TraceIdentifier);
        public IRunner? GetRunner(ISession Session,
            IStoreActiveSessionItem ActiveSessionItem,
            Int32 RunnerNumber, String? TraceIdentifier);
        public ValueTask<IRunner?> GetRunnerAsync(
            ISession Session,
            IStoreActiveSessionItem ActiveSessionItem,
            Int32 RunnerNumber, String? TraceIdentifier, CancellationToken Token
        );
        public Task TerminateSession(ISession Sesion, IStoreActiveSessionItem Session, String? TraceIdentifier);
        public IActiveSessionFeatureImpl AcquireFeatureObject(ISession? Session, String? TraceIdentier, String? Suffix);
        public void ReleaseFeatureObject(IActiveSessionFeatureImpl Feature);
        public ActiveSessionStoreStats? GetCurrentStatistics();
    }
}