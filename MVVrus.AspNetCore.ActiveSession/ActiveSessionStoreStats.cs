namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// A record containing current statistics of ActiveSession store
    /// </summary>
    public record ActiveSessionStoreStats
    {
        /// <summary>
        /// Current active sessions count
        /// </summary>
        public Int32 SessionCount = 0;
        /// <summary>
        /// Current count of existing runners
        /// </summary>
        public Int32 RunnerCount = 0;
        /// <summary>
        /// Current occupated store (in arbitrary units)
        /// </summary>
        public Int32 StoreSize = 0;
    }
}
