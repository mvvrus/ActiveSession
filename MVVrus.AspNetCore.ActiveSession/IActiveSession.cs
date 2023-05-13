namespace MVVrus.AspNetCore.ActiveSession
{
    public interface IActiveSession
    {
        public const int NEW_ACTIVE_SESSION_KEY = -1;

        KeyedActiveSessionRunner<TResult> GetRunner<TRequest, TResult>(TRequest Request);
        IActiveSessionRunner<TResult>? GetRunner<TResult>(int RequestedKey);
        Boolean IsAvailable { get; }
        Boolean IsFresh { get; }
        Task LoadAsync(CancellationToken cancellationToken = default); //TODO Is it really needed?
        Task CommitAsync(CancellationToken cancellationToken = default); //TODO Is it really needed?

    }
}
