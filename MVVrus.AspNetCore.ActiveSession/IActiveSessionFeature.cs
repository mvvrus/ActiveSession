using Microsoft.AspNetCore.Http.Features;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// The feature interface for an ActiveSession feature to be put into a <see cref="HttpContext.Features"/> collection. 
    /// </summary>
    public interface IActiveSessionFeature
    {
        /// <value>
        /// Reference to an <see cref="IActiveSession"/> interface of ActiveSession for this request
        /// </value>
        IActiveSession ActiveSession { get; }

        /// <value>
        /// TODO
        /// </value>
        public Boolean IsLoaded { get; }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public Task LoadAsync(CancellationToken Token=default);

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public Task CommitAsync(CancellationToken Token = default);

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        public ActiveSessionStoreStats? GetCurrentStoreStatistics();

    }
}
