namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// Contains extension methods for <see cref="IActiveSession"/> interface used to create standard runners defined in the ActiveSession library.
    /// </summary>
    public static class StdRunnerActiveSessionExtensions
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TItem">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="Source">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static KeyedRunner<IEnumerable<TItem>> CreateSequenceRunner<TItem>(this IActiveSession Session, 
            EnumAdapterParams<TItem> Source, 
            HttpContext Context)  
        {
            return Session.CreateRunner<EnumAdapterParams<TItem>, IEnumerable<TItem>>(Source,Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TItem">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="Source">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static KeyedRunner<IEnumerable<TItem>> CreateSequenceRunner<TItem>(this IActiveSession Session, 
            IEnumerable<TItem> Source, 
            HttpContext Context)
        {
            return Session.CreateRunner<IEnumerable<TItem>, IEnumerable<TItem>>(Source, Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TItem">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="Source">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static KeyedRunner<IEnumerable<TItem>> CreateSequenceRunner<TItem>(this IActiveSession Session, 
            AsyncEnumAdapterParams<TItem> Source, 
            HttpContext Context)
        {
            return Session.CreateRunner<AsyncEnumAdapterParams<TItem>, IEnumerable<TItem>>(Source, Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TItem">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="Source">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static KeyedRunner<IEnumerable<TItem>> CreateSequenceRunner<TItem>(this IActiveSession Session, 
            IAsyncEnumerable<TItem> Source, 
            HttpContext Context)
        {
            return Session.CreateRunner<IAsyncEnumerable<TItem>, IEnumerable<TItem>>(Source, Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="Source">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static KeyedRunner<IEnumerable<(DateTime, TResult)>> CreateTimeSeriesRunner<TResult>(this IActiveSession Session,
            TimeSeriesParams<TResult> Source, 
            HttpContext Context)
        {
            return Session.CreateRunner<TimeSeriesParams<TResult>, IEnumerable<(DateTime, TResult)>>(Source, Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="Source">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static KeyedRunner<IEnumerable<(DateTime, TResult)>> CreateTimeSeriesRunner<TResult>(this IActiveSession Session,
            ValueTuple<Func<TResult>, TimeSpan> Source,
            HttpContext Context)
        {
            return Session.CreateRunner<ValueTuple<Func<TResult>, TimeSpan>, IEnumerable<(DateTime, TResult)>>(Source, Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="Source">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static KeyedRunner<IEnumerable<(DateTime, TResult)>> CreateTimeSeriesRunner<TResult>(this IActiveSession Session,
            ValueTuple<Func<TResult>, TimeSpan, Int32> Source,
            HttpContext Context)
        {
            return Session.CreateRunner<ValueTuple<Func<TResult>, TimeSpan, Int32>, IEnumerable<(DateTime, TResult)>>(Source, Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="Source">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, TResult> Source,
            HttpContext Context)
        {
            return Session.CreateRunner<Func<Action<TResult, Int32?>, CancellationToken, TResult>, TResult>(Source, Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="Source">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Action<Action<TResult, Int32?>, CancellationToken> Source,
            HttpContext Context)
        {
            return Session.CreateRunner<Action<Action<TResult, Int32?>, CancellationToken>, TResult>(Source, Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TItem">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="RunnerNumber">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static IRunner<IEnumerable<TItem>>? GetSequenceRunner<TItem>(this IActiveSession Session,
            Int32 RunnerNumber,
            HttpContext Context)
        {
            return Session.GetRunner<IEnumerable<TItem>>(RunnerNumber,Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="RunnerNumber">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static IRunner<IEnumerable<(DateTime, TResult)>>? GetTimeSeriesRunner<TResult>(this IActiveSession Session,
            Int32 RunnerNumber,
            HttpContext Context)
        {
            return Session.GetRunner<IEnumerable<(DateTime, TResult)>>(RunnerNumber, Context);
        }

    }
}
