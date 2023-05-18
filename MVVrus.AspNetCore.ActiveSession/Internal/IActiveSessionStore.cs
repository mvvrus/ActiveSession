namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionStore
    {
        public ActiveSession FetchOrCreate(ISession Session);
        public IActiveSessionRunner<TResult> FetchRunner<TResult>(ActiveSession RunnerSession, Int32 KeyRequested);
        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(ActiveSession Session,
            TRequest Request);
        public Task CommitAsync(ActiveSession RunnerSession, CancellationToken cancellationToken);
    }
}