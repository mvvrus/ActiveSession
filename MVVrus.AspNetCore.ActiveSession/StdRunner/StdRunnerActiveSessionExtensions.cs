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
        /// <param name="Gauge">TODO</param>
        /// <param name="Interval">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static KeyedRunner<IEnumerable<(DateTime, TResult)>> CreateTimeSeriesRunner<TResult>(this IActiveSession Session,
            Func<TResult> Gauge, TimeSpan Interval,
            HttpContext Context)
        {
            return Session.CreateRunner<ValueTuple<Func<TResult>, TimeSpan>, IEnumerable<(DateTime, TResult)>>((Gauge, Interval), Context);
        }

        /// <summary>
        ///  TODO
        /// </summary>
        /// <typeparam name="TResult">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="Gauge">TODO</param>
        /// <param name="Interval">TODO</param>
        /// <param name="Count">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns></returns>
        public static KeyedRunner<IEnumerable<(DateTime, TResult)>> CreateTimeSeriesRunner<TResult>(this IActiveSession Session,
            Func<TResult> Gauge, TimeSpan Interval, Int32 Count,
            HttpContext Context)
        {
            return Session.CreateRunner<ValueTuple<Func<TResult>, TimeSpan, Int32>, IEnumerable<(DateTime, TResult)>>
                ((Gauge, Interval, Count), Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="Body">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, TResult> Body,
            HttpContext Context)
        {
            return Session.CreateRunner<Func<Action<TResult, Int32?>, CancellationToken, TResult>, TResult>(Body, Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult">TODO</typeparam>
        /// <param name="Session">TODO</param>
        /// <param name="Body">TODO</param>
        /// <param name="Context">TODO</param>
        /// <returns>TODO</returns>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Action<Action<TResult, Int32?>, CancellationToken> Body,
            HttpContext Context)
        {
            return Session.CreateRunner<Action<Action<TResult, Int32?>, CancellationToken>, TResult>(Body, Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="Session"></param>
        /// <param name="Creator"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, Task<TResult>> Creator,
            HttpContext Context)
        {
            return Session.CreateRunner<Func<Action<TResult, Int32?>, CancellationToken, Task<TResult>>, TResult>(Creator, Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="Session"></param>
        /// <param name="Creator"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, Task> Creator,
            HttpContext Context)
        {
            return Session.CreateRunner<Func<Action<TResult, Int32?>, CancellationToken, Task>, TResult>(Creator, Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="Session"></param>
        /// <param name="Body"></param>
        /// <param name="Context"></param>
        /// <param name="Cts"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <returns></returns>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, TResult> Body,
            HttpContext Context, CancellationTokenSource Cts, Boolean PassCtsOwnership=true)
        {
            return Session.CreateRunner<(Func<Action<TResult, Int32?>, CancellationToken, TResult>, CancellationTokenSource,Boolean), 
                TResult>((Body, Cts, PassCtsOwnership), Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="Session"></param>
        /// <param name="Body"></param>
        /// <param name="Context"></param>
        /// <param name="Cts"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <returns></returns>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Action<Action<TResult, Int32?>, CancellationToken> Body,
            HttpContext Context, CancellationTokenSource Cts, Boolean PassCtsOwnership = true)
        {
            return Session.CreateRunner<(Action<Action<TResult, Int32?>, CancellationToken>, CancellationTokenSource, Boolean),
                TResult>((Body, Cts, PassCtsOwnership), Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="Session"></param>
        /// <param name="Creator"></param>
        /// <param name="Context"></param>
        /// <param name="Cts"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <returns></returns>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, Task<TResult>> Creator,
            HttpContext Context, CancellationTokenSource Cts, Boolean PassCtsOwnership = true)
        {
            return Session.CreateRunner<(Func<Action<TResult, Int32?>, CancellationToken, Task<TResult>>,CancellationTokenSource,Boolean), 
                TResult>((Creator,Cts,PassCtsOwnership), Context);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="Session"></param>
        /// <param name="Creator"></param>
        /// <param name="Context"></param>
        /// <param name="Cts"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <returns></returns>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, Task> Creator,
            HttpContext Context, CancellationTokenSource Cts, Boolean PassCtsOwnership = true)
        {
            return Session.CreateRunner<(Func<Action<TResult, Int32?>, CancellationToken, Task>,CancellationTokenSource,Boolean), 
                TResult>((Creator,Cts,PassCtsOwnership), Context);
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
