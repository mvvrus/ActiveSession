namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionStore
    {
        public ActiveSession FetchOrCreate(ISession Session);
        public IActiveSessionRunner<TResult> FetchRunner<TResult>(ActiveSession Session, int KeyRequested);
        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(ActiveSession Session,
            IServiceProvider Services);
        public Task CommitAsync(ActiveSession Session, CancellationToken cancellationToken);
    }
}