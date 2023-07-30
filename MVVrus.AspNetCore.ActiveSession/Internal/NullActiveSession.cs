namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class NullActiveSession : IActiveSession
    {
        public bool IsAvailable => false;

        public bool IsFresh => true;

        public IServiceProvider SessionServices => throw new NotImplementedException();

        public String Id => "<null session Id>";

        public Task CommitAsync(String? TraceIdentifier, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, String? TraceIdentifier)
        {
            throw new NotImplementedException();
        }

        public IActiveSessionRunner<TResult>? GetRunner<TResult>(int RequestedKey, String? TraceIdentifier)
        {
            return new NullActiveSessionRunner<TResult>();
        }

        public ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(Int32 RequestedKey, String? TraceIdentifier, CancellationToken cancellationToken)
        {
            return new ValueTask<IActiveSessionRunner<TResult>?>(
                Task<IActiveSessionRunner<TResult>?>.FromResult(
                    (IActiveSessionRunner<TResult>?) new NullActiveSessionRunner<TResult>()
                )
            );
        }
    }
}
