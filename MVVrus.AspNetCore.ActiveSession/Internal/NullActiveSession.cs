namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class NullActiveSession : IActiveSession
    {
        public bool IsAvailable => false;

        public bool IsFresh => true;

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request)
        {
            throw new NotImplementedException();
        }

        public IActiveSessionRunner<TResult>? GetRunner<TResult>(int RequestedKey)
        {
            return new NullActiveSessionRunner<TResult>();
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

    }
}
