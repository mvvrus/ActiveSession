namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// An interface that is implemented by session objects of  ActiveSession library - local to each host and attched 
    /// to <see cref="ISession">ASP.NET Core sessions</see> (those are possibly distribute).
    /// </summary>
    /// <remarks>May be implemented as a part of Active Session object 
    /// or as a separate object attached to the ASP.NET Core session </remarks>
    public interface ILocalSession
    {
        /// <summary>
        /// An identifier of this instance 
        /// </summary>
        String Id { get; }

        /// <summary>Indicator showing that this session object is properly initialized and may be used.</summary>
        Boolean IsAvailable { get; }

        /// <summary>The service (DI) container for the scope attached to this sesession</summary>
        IServiceProvider SessionServices { get; }

        /// <summary>Cancellation token that will be fired after the active session completion (eviction/disposal) at start of cleanup/</summary>
        CancellationToken CompletionToken { get; }

        /// <summary>
        /// A set of arbitrary objects associated with this session that are accessible via their string keys.
        /// </summary>
        /// <remarks> Current implementation of this property is based on a <see cref="SortedList{TKey, TValue}"/> class.
        /// </remarks>
        IDictionary<String, Object> Properties { get; }

    }
}
