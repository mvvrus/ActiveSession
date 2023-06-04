namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionStore
    {
        public ActiveSession FetchOrCreateSession(ISession Session, String? TraceIdentifier);
        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(ActiveSession Session,
            TRequest Request, String? TraceIdentifier);
        public IActiveSessionRunner<TResult>? GetRunner<TResult>(ActiveSession RunnerSession, Int32 KeyRequested, String? TraceIdentifier);
        public ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            ActiveSession RunnerSession, 
            Int32 KeyRequested, String? TraceIdentifier, CancellationToken Token
        );
    }
}