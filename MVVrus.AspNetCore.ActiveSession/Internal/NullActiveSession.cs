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

        public ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(Int32 RequestedKey, CancellationToken cancellationToken = default)
        {
            return new ValueTask<IActiveSessionRunner<TResult>?>(
                Task<IActiveSessionRunner<TResult>?>.FromResult(
                    (IActiveSessionRunner<TResult>?) new NullActiveSessionRunner<TResult>()
                )
            );
        }
    }
}
