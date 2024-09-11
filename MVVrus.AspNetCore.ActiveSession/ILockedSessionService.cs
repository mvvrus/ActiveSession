namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Represents an exclusive service accessor for a scoped service from the active session's DI container.
    /// </summary>
    /// <typeparam name="TService">The type of the service to which this accessor was obtained.</typeparam>
    /// <remarks>
    /// Call <see cref="IDisposable.Dispose">Dispose()</see> to release the exclusive service lock represented by this accessor instance.
    /// </remarks>
    public interface ILockedSessionService<TService>: IDisposable
    {
        /// <summary> 
        /// A reference to an instance implementing the service to be accessed exclusively.
        /// </summary>
        TService? Service { get; }
        /// <summary>
        /// A flag showing that an access to the service has been really locked. 
        /// <remarks> Contains <see langword="true"/> if the service was obtained from the active session's DI container,
        /// <see langword="false"/> otherwise. </remarks> 
        /// </summary>
        Boolean IsReallyLocked { get; }
    }
}
