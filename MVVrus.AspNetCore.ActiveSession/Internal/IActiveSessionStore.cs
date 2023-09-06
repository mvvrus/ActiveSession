namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionStore
    {
        public ActiveSession FetchOrCreateSession(ISession Session, String? TraceIdentifier);
        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(ISession Session, ActiveSession RunnerSession,
            TRequest Request, String? TraceIdentifier);
        public IActiveSessionRunner<TResult>? GetRunner<TResult>(ISession Session, ActiveSession RunnerSession, 
            Int32 RunnerNumber, String? TraceIdentifier);
        public ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            ISession Session, ActiveSession RunnerSession, 
            Int32 RunnerNumber, String? TraceIdentifier, CancellationToken Token
        );
        public ActiveSessionFeature CreateFeatureObject(ISession? Session, String? TraceIdentier);
    }
}