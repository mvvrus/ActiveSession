namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>This interface presnts a middleware mapping filter object.</summary>
    /// <remarks>
    /// This is a part of the active session middleware request filtering extensibility feature.
    /// Introduced in v.1.1
    /// </remarks>
    public interface IMiddlewareFilter
    {
        /// <summary>
        /// A minimal value of a filter order implemented by this object.
        /// </summary>
        public Int32 MinOrder { get; }

        /// <summary>
        /// A method that returns a result of application of this filter to the request context.
        /// </summary>
        /// <param name="Context">A request context to which the filter to be applied.</param>
        /// <returns>
        /// The result of the application of the filter to the request context:
        /// <list type="bullet">
        /// <item>
        ///   <see cref="Boolean"/> WasMapped - is the request accepted by the filter;
        /// </item>
        /// <item>
        ///   <see cref="String"/> SessionSuffix - the suffix to be added to the <see cref="ILocalSession.Id"/> property 
        ///   of an active session to which the request will be assigned;
        /// </item>
        /// <item>
        ///   <see cref="Int32"/> Order - the order of the filter that have accepted the request;
        /// </item>
        /// </list>
        /// </returns>
        public (Boolean WasMapped, String? SessionSuffix, Int32 Order) Apply(HttpContext Context);
        /// <summary>
        /// Gets a pretty name for this filter to be written into logs.
        /// </summary>
        /// <returns>A pretty name in question.</returns>
        public String GetPrettyName() { return "<unspecified filter>"; }
    }

}

