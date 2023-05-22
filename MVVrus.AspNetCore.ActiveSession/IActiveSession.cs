using System.Threading.Tasks;

namespace MVVrus.AspNetCore.ActiveSession
{
    public interface IActiveSession
    {
        public const int NEW_ACTIVE_SESSION_KEY = -1;

        KeyedActiveSessionRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request);
        IActiveSessionRunner<TResult>? GetRunner<TResult>(int RequestedKey);
        ValueTask<IActiveSessionRunner<TResult>?> GetRunnerAsync<TResult>(
            int RequestedKey, 
            CancellationToken cancellationToken = default
        );
        Boolean IsAvailable { get; }
        Boolean IsFresh { get; }
        Task CommitAsync(CancellationToken CancellationToken = default); 
    }
}
