
namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class NullActiveSession : IActiveSession
    {
        const String MESSAGE = "Invalid operation: an ActiveSession is not available";
        public bool IsAvailable => false;

        public bool IsFresh => true;

        public IServiceProvider SessionServices => throw new InvalidOperationException(MESSAGE);

        public String Id => "<null session Id>";

        public CancellationToken CompletionToken => CancellationToken.None;

        public Task CleanupCompletionTask => throw new InvalidOperationException(MESSAGE);

        public Int32 Generation => throw new NotImplementedException();

        public Boolean IsIdle =>  true;

        public IDictionary<String, Object> Properties => throw new NotImplementedException();

        public Task CommitAsync(String? TraceIdentifier, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public KeyedRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, HttpContext Context)
        {
            throw new InvalidOperationException(MESSAGE);
        }

        public IRunner<TResult>? GetRunner<TResult>(int RequestedKey, HttpContext Context)
        {
            throw new InvalidOperationException(MESSAGE);
        }

        public Task<IRunner<TResult>?> GetRunnerAsync<TResult>(Int32 RequestedKey, HttpContext Context, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(MESSAGE);
        }

        public Task Terminate(HttpContext Context)
        {
            throw new InvalidOperationException(MESSAGE);
        }

        public ValueTask<Boolean> WaitUntilIdle(Boolean AbortAll, TimeSpan Timeout)
        {
            throw new NotImplementedException();
        }
    }
}
