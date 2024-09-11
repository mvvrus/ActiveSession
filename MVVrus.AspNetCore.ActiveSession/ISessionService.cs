namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// This generic interface facilitates obtaining a service instance 
    /// from the current active session's service scope DI container(if any).
    /// </summary>
    /// <typeparam name="TService">Type of the service to be obtained. </typeparam>
    /// <remarks>
    /// <para>
    /// This interface is registered as a scoped service in the application's DI container 
    /// as a part of registering the ActivesSession infrastracture services.
    /// It is designed to be obtained from a request's scope.
    /// </para>
    /// <para>
    /// If no active session for the request is available, the service resolution process performed by this ISessionService 
    /// service falls back to the request's scope DI container and obtains a service from it.  
    /// This is done for compatibility reason: a service obtained this way may be used by a request handler 
    /// irrespective of existence of an active session for the request.
    /// See also <see cref="IsFromSession"/> property description.
    /// </para>
    /// </remarks>
    public interface ISessionService<TService>
    {
        /// <summary> 
        /// A reference to an instance implementing the service.
        /// </summary>
        TService? Service { get; }
        /// <summary>
        /// Contains <see langword="true" /> if the service has been obtained from active session's DI container,
        /// <see langword="false"/> otherwise.
        /// </summary>
        Boolean IsFromSession { get; }
    }
}
