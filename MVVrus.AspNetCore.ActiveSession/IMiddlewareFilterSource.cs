namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>This interface presnts a source object for a middleware mapping filter.</summary>
    /// <remarks>
    /// This is a part of the active session middleware request filtering extensibility feature.
    /// Introduced in v.1.1
    /// </remarks>
    public interface IMiddlewareFilterSource
    {
        /// <summary>
        /// Creates a middleware filter object from this source.
        /// </summary>
        /// <param name="Order">An order of the filter object in an ActiveSession filter list.</param>
        /// <returns>The middleware filter object created.</returns>
        public IMiddlewareFilter Create(Int32 Order);
        /// <summary>
        /// Indication that a middleware filter created from this source may assign a suffix to an active session application.
        /// </summary>
        public Boolean HasSuffix { get; }
        /// <summary>
        /// Gets a pretty name for this source to be written into logs.
        /// </summary>
        /// <returns>A pretty name in question.</returns>
        public String GetPrettyName() { return "<unspecified filter>"; }
    }
}


