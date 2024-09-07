namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <typeparam name="TService">Type of the service to be obtained </typeparam>
    public interface ILockedSessionService<TService>: IDisposable
    {
        /// <summary> 
        /// A reference to an instance implementing the service.
        /// </summary>
        TService? Service { get; }
        /// <summary>
        /// A flag showing that the service has been really locked. 
        /// <remarks>If a service was not obtained from the active session container this flag would always be <see langword="false"/> </remarks> 
        /// </summary>
        Boolean IsReallyLocked { get; }
    }
}
