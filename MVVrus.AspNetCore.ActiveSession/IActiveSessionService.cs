namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// This generic interfase is intended to extract service implementations 
    /// from DI container defined in  the active session scope
    /// </summary>
    /// <typeparam name="TService">Type of service to be extracted </typeparam>
    /// <remarks>
    /// If no active session is available for the request the interface falls back to the request scope DI container
    /// The real source of the service may be determined via <see cref="IsFromSession"/> property.
    /// </remarks>
    public interface IActiveSessionService<TService>
    {
        /// <value> 
        /// 
        /// </value>
        TService? Service { get; }
        /// <summary>
        /// 
        /// </summary>
        Boolean IsFromSession { get; }
    }
}
