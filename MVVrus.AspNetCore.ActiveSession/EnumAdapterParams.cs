namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// Class containg parametrs to pass to a <see cref="EnumAdapterRunner{TResult}"/> constructor
    /// </summary>
    public record struct EnumAdapterParams<TResult>
    {
        /// <value>
        /// The base based object implementing <see cref="IEnumerable{T}"/> for which the adapter to be created
        /// </value>
        public IEnumerable<TResult> AdapterBase { get; set; }
    }
}
