
namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class NullActiveSession : NullLocalSession, IStoreActiveSessionItem
    {
        public bool IsFresh => true;

        public String BaseId => "<null session BaseId>";

        public CancellationToken CompletionToken => CancellationToken.None;

        public Task CleanupCompletionTask => throw new InvalidOperationException(MESSAGE);

        public Int32 Generation => -1;

        public IRunnerManager RunnerManager => throw new NotImplementedException();

        public IStoreGroupItem? BaseGroup => null;

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

        public ValueTask<IRunner<TResult>?> GetRunnerAsync<TResult>(Int32 RequestedKey, HttpContext Context, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(MESSAGE);
        }

        public Task? TrackRunnerCleanup(Int32 RunnerNumber)
        {
            throw new InvalidOperationException(MESSAGE);
        }

        public Task Terminate(HttpContext Context)
        {
            throw new InvalidOperationException(MESSAGE);
        }

        public IRunner? GetNonTypedRunner(Int32 RunnerNumber, HttpContext Context)
        {
            throw new InvalidOperationException(MESSAGE);
        }

        public ValueTask<IRunner?> GetNonTypedRunnerAsync(Int32 RunnerNumber, HttpContext Context, CancellationToken CancellationToken = default)
        {
            throw new InvalidOperationException(MESSAGE);
        }

        public Task<Boolean> WaitForServiceAsync(Type ServiceType, TimeSpan Timeout, CancellationToken Token)
        {
            throw new NotImplementedException();
        }

        public void ReleaseService(Type ServiceType)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
