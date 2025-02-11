using Microsoft.AspNetCore.Http.Features;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The feature interface for an ActiveSession feature to be put into a <see cref="HttpContext.Features"/> collection. 
    /// </summary>
    public interface IActiveSessionFeature
    {
        /// <summary>
        /// Points to an Active Session object for this request.
        /// </summary>
        /// <remarks><see cref="IActiveSession">Active Session objects</see> serves as a means 
        /// of creating, searching and controlling <see cref="IRunner{TResult}">runners</see> available to handlers 
        /// of requests pertaining to the current <see cref="ISession">ASP.NET Core session</see>.
        /// They, like <see cref="ILocalSession">Local Session objects</see>, 
        /// can also contain a scoped service (DI) container and a dictionary of named objects, associated with the session.
        /// A number of different Active Session objects can be associated with a whole ASP.NET Core session, 
        /// but any specific request handler can have acess to the only Active Session object, 
        /// that can be terminated and renewed between requests, and, generally speaking,  
        /// selected on the basis of the request properties (but this feature is not implemented yet).
        /// </remarks>
        IActiveSession ActiveSession { get; }

        /// <summary>
        /// Points to an Local Session object for this request.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="IActiveSession">Active Session objects</see> 
        /// <see cref="ILocalSession">Local Session objects</see> cannot be used 
        /// to create, search for an control <see cref="IRunner{TResult}">runners</see>. 
        /// They can only contain a scoped service (DI) container and a dictionary of named objects, associated with the session.
        /// Also at most one Local Session object can be associated with an <see cref="ISession">ASP.NET Core session</see></remarks>
        ILocalSession LocalSession { get; }

        /// <summary>
        /// Indicates that <see cref="ActiveSession"/> property contains reference 
        /// to an initialized ActiveSession-imlementing object (which may be dummy one).
        /// </summary>
        public Boolean IsLoaded { get; }

        /// <summary>
        /// Asynchronously initializes all <see cref="ActiveSession"/> stuff.
        /// </summary>
        /// <param name="Token">Can be used to cancel <see cref="ActiveSession"/> initialization operations.</param>
        /// <returns>Task that may be used to observe the initialization.</returns>
        public Task LoadAsync(CancellationToken Token=default);

        /// <summary>
        /// Asynchronously commits all changes related to the ActiveSession-imlementing object.
        /// </summary>
        /// <param name="Token">Can be used to cancel ActiveSession-related commit operations.</param>
        /// <returns>Task that may be used to observe the commit operation.</returns>
        public Task CommitAsync(CancellationToken Token = default);

        /// <summary>
        /// Get current ActiveSession store usage statistics.
        /// </summary>
        /// <returns>A record containing ActiveSession store usage statistics if it exists, otherwise <see langword="null"/></returns>
        public ActiveSessionStoreStats? GetCurrentStoreStatistics();

        /// <summary>
        /// Tries to update an active session object for this request if the previous one has been terminated.
        /// </summary>
        /// <returns>A boolean value indicating was the active session object really changed for this request.</returns>
        public Boolean RefreshActiveSession();

    }
}
