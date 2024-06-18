using Microsoft.AspNetCore.Http.Features;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The feature interface for an ActiveSession feature to be put into a <see cref="HttpContext.Features"/> collection. 
    /// </summary>
    public interface IActiveSessionFeature
    {
        /// <summary>
        /// Points to an Active Session for this request.
        /// </summary>
        IActiveSession ActiveSession { get; }

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

    }
}
