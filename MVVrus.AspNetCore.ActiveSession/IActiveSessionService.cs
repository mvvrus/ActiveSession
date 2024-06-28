namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// This generic interface is intended to obtain a service implementation 
    /// from a DI container in the service scope bound to the current active session (if any).
    /// </summary>
    /// <typeparam name="TService">Type of the service to be obtained </typeparam>
    /// <remarks>
    /// If no active session is currently available, 
    /// the process of receiving a service implementation falls back to the DI container associated with the request scope.
    /// The real source of the service implementation may be determined via <see cref="IsFromSession"/> property.
    /// </remarks>
    public interface IActiveSessionService<TService>
    {
        /// <summary> 
        /// A reference to an instance implementing the service.
        /// </summary>
        TService? Service { get; }
        /// <summary>
        /// A flag showing that the service have been really obtained from the active session scope DI container.
        /// </summary>
        Boolean IsFromSession { get; }
    }
}
