namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class NullActiveSession : IActiveSession
    {
        public bool IsAvailable => false;

        public bool IsFresh => true;

        public IServiceProvider SessionServices => throw new NotImplementedException();

        public String Id => "<null session Id>";

        public CancellationToken CompletionToken => CancellationToken.None;

        public Task<Boolean> CleanupCompletionTask => throw new NotImplementedException();

        public Task CommitAsync(String? TraceIdentifier, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, HttpContext Context)
        {
            throw new NotImplementedException();
        }

        public IActiveSessionRunner<TResult>? GetRunner<TResult>(int RequestedKey, HttpContext Context)
        {
            return new NullActiveSessionRunner<TResult>();
        }

        public ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(Int32 RequestedKey, HttpContext Context, CancellationToken cancellationToken)
        {
            return new ValueTask<IActiveSessionRunner<TResult>?>(
                Task<IActiveSessionRunner<TResult>?>.FromResult(
                    (IActiveSessionRunner<TResult>?) new NullActiveSessionRunner<TResult>()
                )
            );
        }

        public Task<Boolean> Terminate(Boolean Global = false)
        {
            throw new NotImplementedException();
        }
    }
}
