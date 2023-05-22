namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionStore
    {
        public ActiveSession FetchOrCreate(ISession Session);
        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(ActiveSession Session,
            TRequest Request);
        public IActiveSessionRunner<TResult>? GetRunner<TResult>(ActiveSession RunnerSession, Int32 KeyRequested);
        public ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            ActiveSession RunnerSession, 
            Int32 KeyRequested, CancellationToken Token
        );
    }
}