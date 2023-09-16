namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionStore
    {
        public IActiveSession FetchOrCreateSession(ISession Session, String? TraceIdentifier);
        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(ISession Session, IRunnerManager NumberManager,
            TRequest Request, String? TraceIdentifier);
        public IActiveSessionRunner<TResult>? GetRunner<TResult>(ISession Session, IRunnerManager NumberManager,
            Int32 RunnerNumber, String? TraceIdentifier);
        public ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            ISession Session, IRunnerManager NumberManager, 
            Int32 RunnerNumber, String? TraceIdentifier, CancellationToken Token
        );
        public IActiveSessionFeature CreateFeatureObject(ISession? Session, String? TraceIdentier);
    }
}