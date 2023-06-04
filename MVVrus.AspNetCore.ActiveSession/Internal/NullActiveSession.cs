namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class NullActiveSession : IActiveSession
    {
        public bool IsAvailable => false;

        public bool IsFresh => true;

        public Task CommitAsync(CancellationToken cancellationToken, String? TraceIdent)
        {
            return Task.CompletedTask;
        }

        public KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, HttpContext? Context)
        {
            throw new NotImplementedException();
        }

        public IActiveSessionRunner<TResult>? GetRunner<TResult>(int RequestedKey, HttpContext? Context)
        {
            return new NullActiveSessionRunner<TResult>();
        }

        public ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(Int32 RequestedKey, HttpContext? Context, CancellationToken cancellationToken)
        {
            return new ValueTask<IActiveSessionRunner<TResult>?>(
                Task<IActiveSessionRunner<TResult>?>.FromResult(
                    (IActiveSessionRunner<TResult>?) new NullActiveSessionRunner<TResult>()
                )
            );
        }
    }
}
