namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// TODO
    /// </summary>
    public interface ISessionServiceLock<TService>
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Token"></param>
        /// <param name="Timeout"></param>
        /// <returns></returns>
        Task<ILockedSessionService<TService>?> AcquireAsync(TimeSpan Timeout, CancellationToken Token);
    }
}
