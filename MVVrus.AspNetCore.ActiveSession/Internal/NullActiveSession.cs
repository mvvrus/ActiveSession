
namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class NullActiveSession : NullLocalSession, IActiveSession
    {
        public bool IsFresh => true;

        public String Id => "<null session Id>";

        public CancellationToken CompletionToken => CancellationToken.None;

        public Task CleanupCompletionTask => throw new InvalidOperationException(MESSAGE);

        public Int32 Generation => throw new NotImplementedException();

        public Boolean IsIdle =>  true;

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

        public Task? TrackRunnerCleanup(Int32 RunnerNumber)
        {
            throw new NotImplementedException();
        }

        public Task Terminate(HttpContext Context)
        {
            throw new InvalidOperationException(MESSAGE);
        }

        public ValueTask<Boolean> WaitUntilIdle(Boolean AbortAll, TimeSpan Timeout, CancellationToken Token=default)
        {
            throw new NotImplementedException();
        }
    }
}
