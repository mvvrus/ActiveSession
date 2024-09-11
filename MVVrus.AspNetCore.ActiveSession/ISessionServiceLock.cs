namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// This generic interface is intended to obtain an exclusive access to a scoped service instance 
    /// within the current active session's service scope (if any).
    /// </summary>
    /// <typeparam name="TService"> Type of the service to which the exclusive access to be obtained</typeparam>
    /// <remarks>
    /// <para>
    /// This interface is registered as a scoped service in the application's DI container 
    /// as a part of registering the ActivesSession infrastracture services.
    /// It is designed to be obtained from a request's scope.
    /// </para>
    /// <para>
    /// If no active session for a request is available, the process  of obtaining an exclusive accessor for a service 
    /// performed by this ISessionServiceLock service falls back to the request's scope DI container.
    /// This is done for compatibility reason: a service accessor obtained this way may be used by a request handler 
    /// irrespective of existence of an active session for the request.
    /// Because scoped service instances from request DI containers are never shared between requests,
    /// the service accessor <see cref="ILockedSessionService{TService}"/> obtained from the request's scope DI container 
    /// doesn't really represent any lock, so many of such accessors for different requests may be obtained simultaneously.
    /// </para>
    /// </remarks>
    public interface ISessionServiceLock<TService>
    {
        /// <summary>
        /// Obtains an exclusive service accessor asynchronously.
        /// </summary>
        /// <param name="Timeout">Timeout for the process of obtaining  an exclusive service accessor.</param>
        /// <param name="Token">May be used for cancellation of the process of obtaining  an exclusive service accessor.</param>
        /// <returns>
        /// A task representing the process of obtaining an exclusive service accessor. The task result will be 
        /// the exclusive service accessor obtained or <see langword="null"/> if the timeout expires.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Will be thrown when the associated active session is disposed during the accessor obtaining process.
        /// </exception>
        /// <exception cref="TaskCanceledException">Will be thrown when the accessor obtaining process is cancelled.</exception>
        /// <remarks>
        /// If a lock was put on a requested service by previous call of this method, 
        /// the the process of obtaining an exclusive accessor will wait until the lock is released 
        /// by disposing the previously obtained exclusive accessor or until the specified <paramref name="Timeout"/> expires. 
        /// This process can also be canceled via <paramref name="Token"/> cancellation token.
        /// </remarks>
        Task<ILockedSessionService<TService>?> AcquireAsync(TimeSpan Timeout, CancellationToken Token);
    }
}
