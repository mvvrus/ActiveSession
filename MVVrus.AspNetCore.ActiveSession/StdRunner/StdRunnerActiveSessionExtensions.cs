namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// Contains extension methods for <see cref="IActiveSession"/> interface 
    /// used to create standard runners defined in the ActiveSession library.
    /// </summary>
    public static class StdRunnerActiveSessionExtensions
    {
        /// <summary>
        /// Creates an <see cref="EnumAdapterRunner{TItem}"/> instance in the specified Active Session
        /// </summary>
        /// <typeparam name="TItem">
        /// <inheritdoc cref="EnumerableRunnerBase{TItem}" path='/typeparam[@name="TItem"]'/>
        /// </typeparam>
        /// <param name="Session">An interface of Active Session object to work with.</param>
        /// <param name="Source">
        /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(EnumAdapterParams{TItem}, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger{EnumAdapterRunner{TItem}}?)" path='/param[@name="Params"]' />
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        public static KeyedRunner<IEnumerable<TItem>> CreateSequenceRunner<TItem>(this IActiveSession Session, 
            EnumAdapterParams<TItem> Source, 
            HttpContext Context)  
        {
            return Session.CreateRunner<EnumAdapterParams<TItem>, IEnumerable<TItem>>(Source,Context);
        }

        /// <summary>
        /// <inheritdoc cref="StdRunnerActiveSessionExtensions.CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path="/summary"/>
        /// </summary>
        /// <typeparam name="TItem">
        /// <inheritdoc cref="EnumerableRunnerBase{TItem}" path='/typeparam[@name="TItem"]'/>
        /// </typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="Source">
        /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger{EnumAdapterRunner{TItem}}?)" path='/param[@name="Source"]' />
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        public static KeyedRunner<IEnumerable<TItem>> CreateSequenceRunner<TItem>(this IActiveSession Session, 
            IEnumerable<TItem> Source, 
            HttpContext Context)
        {
            return Session.CreateRunner<IEnumerable<TItem>, IEnumerable<TItem>>(Source, Context);
        }

        /// <summary>
        /// Creates an <see cref="AsyncEnumAdapterRunner{TItem}"/> instance in the specified Active Session.
        /// </summary>
        /// <typeparam name="TItem">
        /// <inheritdoc cref="EnumerableRunnerBase{TItem}" path='/typeparam[@name="TItem"]'/>
        /// </typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="Source">
        /// <inheritdoc cref="AsyncEnumAdapterRunner{TItem}.AsyncEnumAdapterRunner(AsyncEnumAdapterParams{TItem}, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger{AsyncEnumAdapterRunner{TItem}}?)" path='/param[@name="Params"]' />
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        public static KeyedRunner<IEnumerable<TItem>> CreateSequenceRunner<TItem>(this IActiveSession Session, 
            AsyncEnumAdapterParams<TItem> Source, 
            HttpContext Context)
        {
            return Session.CreateRunner<AsyncEnumAdapterParams<TItem>, IEnumerable<TItem>>(Source, Context);
        }

        /// <summary>
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, AsyncEnumAdapterParams{TItem}, HttpContext)" path="/summary"/>
        /// </summary>
        /// <typeparam name="TItem">
        /// <inheritdoc cref="EnumerableRunnerBase{TItem}" path='/typeparam[@name="TItem"]'/>
        /// </typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="Source">
        /// <inheritdoc cref="AsyncEnumAdapterRunner{TItem}.AsyncEnumAdapterRunner(IAsyncEnumerable{TItem}, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger{AsyncEnumAdapterRunner{TItem}}?)" path='/param[@name="Source"]' />
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        public static KeyedRunner<IEnumerable<TItem>> CreateSequenceRunner<TItem>(this IActiveSession Session, 
            IAsyncEnumerable<TItem> Source, 
            HttpContext Context)
        {
            return Session.CreateRunner<IAsyncEnumerable<TItem>, IEnumerable<TItem>>(Source, Context);
        }

        /// <summary>
        /// Creates a <see cref="TimeSeriesRunner{TResult}"/> instance in the specified Active Session.
        /// </summary>
        /// <typeparam name="TResult">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}" path='/typeparam[@name="TResult"]'/>
        /// </typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="SeriesParam">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(TimeSeriesParams{TResult}, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger{TimeSeriesRunner{TResult}}?)" path='/param[@name="SeriesParam"]'/>
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        public static KeyedRunner<IEnumerable<(DateTime, TResult)>> CreateTimeSeriesRunner<TResult>(this IActiveSession Session,
            TimeSeriesParams<TResult> SeriesParam, 
            HttpContext Context)
        {
            return Session.CreateRunner<TimeSeriesParams<TResult>, IEnumerable<(DateTime, TResult)>>(SeriesParam, Context);
        }

        /// <summary>
        /// <inheritdoc cref="CreateTimeSeriesRunner{TResult}(IActiveSession, TimeSeriesParams{TResult}, HttpContext)" path="/summary"/>
        /// Creates a runner producing an unlimited series of measurements.
        /// </summary>
        /// <typeparam name="TResult">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}" path='/typeparam[@name="TResult"]'/>
        /// </typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="Gauge">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Gauge"]'/>
        /// </param>
        /// <param name="Interval">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Interval"]'/>
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        /// <remarks>
        /// </remarks>
        public static KeyedRunner<IEnumerable<(DateTime, TResult)>> CreateTimeSeriesRunner<TResult>(this IActiveSession Session,
            Func<TResult> Gauge, TimeSpan Interval,
            HttpContext Context)
        {
            return Session.CreateRunner<ValueTuple<Func<TResult>, TimeSpan>, IEnumerable<(DateTime, TResult)>>((Gauge, Interval), Context);
        }

        /// <summary>
        /// <inheritdoc cref="CreateTimeSeriesRunner{TResult}(IActiveSession, TimeSeriesParams{TResult}, HttpContext)" path="/summary"/>
        /// Creates a runner producing a finite series of measurements.
        /// </summary>
        /// <typeparam name="TResult">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}" path='/typeparam[@name="TResult"]'/>
        /// </typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="Gauge">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Gauge"]'/>
        /// </param>
        /// <param name="Interval">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Interval"]'/>
        /// </param>
        /// <param name="Count">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, Microsoft.Extensions.Options.IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Count"]'/>
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        public static KeyedRunner<IEnumerable<(DateTime, TResult)>> CreateTimeSeriesRunner<TResult>(this IActiveSession Session,
            Func<TResult> Gauge, TimeSpan Interval, Int32 Count,
            HttpContext Context)
        {
            return Session.CreateRunner<ValueTuple<Func<TResult>, TimeSpan, Int32>, IEnumerable<(DateTime, TResult)>>
                ((Gauge, Interval, Count), Context);
        }

        /// <summary>
        /// <general>Creates a <see cref="SessionProcessRunner{TResult}"/> instance in the specified Active Session: </general>
        /// synchronous task, returns result, no external source for CompletionToken.
        /// </summary>
        /// <typeparam name="TResult"><inheritdoc cref="SessionProcessRunner{TResult}"  path='/typeparam[@name="TResult"]'/></typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="ProcessTaskBody">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/param[@name="ProcessTaskBody"]'/>
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        /// <remarks>
        /// <features>Runners created by this method have the following features.</features>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/summary/*[not(self::factory)]'/>
        /// </remarks>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, TResult> ProcessTaskBody,
            HttpContext Context)
        {
            return Session.CreateRunner<Func<Action<TResult, Int32?>, CancellationToken, TResult>, TResult>(ProcessTaskBody, Context);
        }

        /// <summary>
        /// <inheritdoc cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)" path="/summary/general"/>
        /// synchronous task, does not return result, no external source for CompletionToken.
        /// </summary>
        /// <typeparam name="TResult"><inheritdoc cref="SessionProcessRunner{TResult}"  path='/typeparam[@name="TResult"]'/></typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="ProcessTaskBody"><inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Action{Action{TResult, int?}, CancellationToken}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/param[@name="ProcessTaskBody"]'/></param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        /// <remarks>
        /// <inheritdoc path='/remarks/features' cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)"/>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Action{Action{TResult, int?}, CancellationToken}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/summary/*[not(self::factory)]'/>
        /// </remarks>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Action<Action<TResult, Int32?>, CancellationToken> ProcessTaskBody,
            HttpContext Context)
        {
            return Session.CreateRunner<Action<Action<TResult, Int32?>, CancellationToken>, TResult>(ProcessTaskBody, Context);
        }

        /// <summary>
        /// <inheritdoc cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)" path="/summary/general"/>
        /// asynchronous task, returns result, no external source for CompletionToken.
        /// </summary>
        /// <typeparam name="TResult"><inheritdoc cref="SessionProcessRunner{TResult}"  path='/typeparam[@name="TResult"]'/></typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="ProcessTaskCreator">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task{TResult}}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/param[@name="ProcessTaskCreator"]'/>
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        /// <remarks>
        /// <inheritdoc path='/remarks/features' cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)"/>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task{TResult}}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/summary/*[not(self::factory)]'/>
        /// </remarks>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, Task<TResult>> ProcessTaskCreator,
            HttpContext Context)
        {
            return Session.CreateRunner<Func<Action<TResult, Int32?>, CancellationToken, Task<TResult>>, TResult>(ProcessTaskCreator, Context);
        }

        /// <summary>
        /// <inheritdoc cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)" path="/summary/general"/>
        /// asynchronous task, does not return result, no external source for CompletionToken.
        /// </summary>
        /// <typeparam name="TResult"><inheritdoc cref="SessionProcessRunner{TResult}"  path='/typeparam[@name="TResult"]'/></typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="ProcessTaskCreator">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/param[@name="ProcessTaskCreator"]'/>
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        /// <remarks>
        /// <inheritdoc path='/remarks/features' cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)"/>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/summary/*[not(self::factory)]'/>
        /// </remarks>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, Task> ProcessTaskCreator,
            HttpContext Context)
        {
            return Session.CreateRunner<Func<Action<TResult, Int32?>, CancellationToken, Task>, TResult>(ProcessTaskCreator, Context);
        }

        /// <summary>
        /// <general>Creates a <see cref="SessionProcessRunner{TResult}"/> instance in the specified Active Session: </general>
        /// synchronous task, returns result, can use externalsource for CompletionToken.
        /// </summary>
        /// <typeparam name="TResult"><inheritdoc cref="SessionProcessRunner{TResult}"  path='/typeparam[@name="TResult"]'/></typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="ProcessTaskBody">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/param[@name="ProcessTaskBody"]'/>
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <param name="CompletionTokenSource">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="CompletionTokenSource"]'/>
        /// </param>
        /// <param name="PassCtsOwnership">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="PassCtsOwnership"]'/>
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        /// <remarks>
        /// <inheritdoc path='/remarks/features' cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)"/>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(ValueTuple{Func{Action{TResult, int?}, CancellationToken, TResult}, CancellationTokenSource?, bool}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/summary/*[not(self::factory)]'/>
        /// </remarks>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, TResult> ProcessTaskBody,
            HttpContext Context, CancellationTokenSource CompletionTokenSource, Boolean PassCtsOwnership=true)
        {
            return Session.CreateRunner<(Func<Action<TResult, Int32?>, CancellationToken, TResult>, CancellationTokenSource,Boolean), 
                TResult>((ProcessTaskBody, CompletionTokenSource, PassCtsOwnership), Context);
        }

        /// <summary>
        /// <inheritdoc cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)" path="/summary/general"/>
        /// synchronous task, does not return result, can use externalsource for CompletionToken.
        /// </summary>
        /// <typeparam name="TResult"><inheritdoc cref="SessionProcessRunner{TResult}"  path='/typeparam[@name="TResult"]'/></typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="ProcessTaskBody"><inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Action{Action{TResult, int?}, CancellationToken}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/param[@name="ProcessTaskBody"]'/></param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <param name="CompletionTokenSource">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="CompletionTokenSource"]'/>
        /// </param>
        /// <param name="PassCtsOwnership">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="PassCtsOwnership"]'/>
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        /// <remarks>
        /// <inheritdoc path='/remarks/features' cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)"/>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(ValueTuple{Action{Action{TResult, int?}, CancellationToken}, CancellationTokenSource?, bool}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/summary/*[not(self::factory)]'/>
        /// </remarks>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Action<Action<TResult, Int32?>, CancellationToken> ProcessTaskBody,
            HttpContext Context, CancellationTokenSource CompletionTokenSource, Boolean PassCtsOwnership = true)
        {
            return Session.CreateRunner<(Action<Action<TResult, Int32?>, CancellationToken>, CancellationTokenSource, Boolean),
                TResult>((ProcessTaskBody, CompletionTokenSource, PassCtsOwnership), Context);
        }

        /// <summary>
        /// <inheritdoc cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)" path="/summary/general"/>
        /// asynchronous task, returns result, can use external source for CompletionToken.
        /// </summary>
        /// <typeparam name="TResult"><inheritdoc cref="SessionProcessRunner{TResult}"  path='/typeparam[@name="TResult"]'/></typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="ProcessTaskCreator">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task{TResult}}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/param[@name="ProcessTaskCreator"]'/>
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <param name="CompletionTokenSource">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="CompletionTokenSource"]'/>
        /// </param>
        /// <param name="PassCtsOwnership">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="PassCtsOwnership"]'/>
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        /// <remarks>
        /// <inheritdoc path='/remarks/features' cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)"/>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(ValueTuple{Func{Action{TResult, int?}, CancellationToken, Task{TResult}}, CancellationTokenSource?, bool}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/summary/*[not(self::factory)]'/>
        /// </remarks>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, Task<TResult>> ProcessTaskCreator,
            HttpContext Context, CancellationTokenSource CompletionTokenSource, Boolean PassCtsOwnership = true)
        {
            return Session.CreateRunner<(Func<Action<TResult, Int32?>, CancellationToken, Task<TResult>>,CancellationTokenSource,Boolean), 
                TResult>((ProcessTaskCreator,CompletionTokenSource,PassCtsOwnership), Context);
        }

        /// <summary>
        /// <inheritdoc cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)" path="/summary/general"/>
        /// asynchronous task, does not return result, can use externalsource for CompletionToken.
        /// </summary>
        /// <typeparam name="TResult"><inheritdoc cref="SessionProcessRunner{TResult}"  path='/typeparam[@name="TResult"]'/></typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="ProcessTaskCreator">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/param[@name="ProcessTaskCreator"]'/>
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <param name="CompletionTokenSource">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="CompletionTokenSource"]'/>
        /// </param>
        /// <param name="PassCtsOwnership">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="PassCtsOwnership"]'/>
        /// </param>
        /// <returns>
        /// <inheritdoc cref="IActiveSession.CreateRunner{TRequest, TResult}(TRequest, HttpContext)" path='/returns' />
        /// </returns>
        /// <remarks>
        /// <inheritdoc path='/remarks/features' cref="CreateSessionProcessRunner{TResult}(IActiveSession, Func{Action{TResult, int?}, CancellationToken, TResult}, HttpContext)"/>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(ValueTuple{Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)" path='/summary/*[not(self::factory)]'/>
        /// </remarks>
        public static KeyedRunner<TResult> CreateSessionProcessRunner<TResult>(this IActiveSession Session,
            Func<Action<TResult, Int32?>, CancellationToken, Task> ProcessTaskCreator,
            HttpContext Context, CancellationTokenSource CompletionTokenSource, Boolean PassCtsOwnership = true)
        {
            return Session.CreateRunner<(Func<Action<TResult, Int32?>, CancellationToken, Task>,CancellationTokenSource,Boolean), 
                TResult>((ProcessTaskCreator,CompletionTokenSource,PassCtsOwnership), Context);
        }

        /// <summary>
        /// Searches for an existing runner with result type <see cref="IEnumerable{T}">IEnumerable&lt;TItem&gt;</see>
        /// i.e. instance of class <see cref="EnumAdapterRunner{TItem}">EnumAdapterRunner&lt;TItem&gt;</see> 
        /// or <see cref="AsyncEnumAdapterRunner{TItem}">AsyncEnumAdapterRunner&lt;TItem&gt;</see>.
        /// </summary>
        /// <typeparam name="TItem">
        /// <inheritdoc cref="EnumerableRunnerBase{TItem}" path='/typeparam[@name="TItem"]'/>
        /// </typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="RunnerNumber">
        /// <inheritdoc cref="IActiveSession.GetRunner{TResult}(int, HttpContext)" path='/param[@name="RunnerNumber"]' />
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.GetRunner{TResult}(int, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns><inheritdoc cref="IActiveSession.GetRunner{TResult}(int, HttpContext)" path='/returns' /> </returns>
        public static IRunner<IEnumerable<TItem>>? GetSequenceRunner<TItem>(this IActiveSession Session,
            Int32 RunnerNumber,
            HttpContext Context)
        {
            return Session.GetRunner<IEnumerable<TItem>>(RunnerNumber,Context);
        }

        /// <summary>
        /// Searches for an existing runner with result type <see cref="IEnumerable{T}">IEnumerable&lt;(DateTime, TResult)&gt;</see>
        /// i.e. instance of class <see cref="TimeSeriesRunner{TResult}">TimeSeriesRunner&lt;TResult&gt;</see>. 
        /// </summary>
        /// <typeparam name="TResult">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}" path='/typeparam[@name="TResult"]'/>
        /// </typeparam>
        /// <param name="Session">
        /// <inheritdoc cref="CreateSequenceRunner{TItem}(IActiveSession, EnumAdapterParams{TItem}, HttpContext)" path='/param[@name="Session"]'/>
        /// </param>
        /// <param name="RunnerNumber">
        /// <inheritdoc cref="IActiveSession.GetRunner{TResult}(int, HttpContext)" path='/param[@name="RunnerNumber"]' />
        /// </param>
        /// <param name="Context">
        /// <inheritdoc cref="IActiveSession.GetRunner{TResult}(int, HttpContext)" path='/param[@name="Context"]' />
        /// </param>
        /// <returns><inheritdoc cref="IActiveSession.GetRunner{TResult}(int, HttpContext)" path='/returns' /> </returns>
        public static IRunner<IEnumerable<(DateTime, TResult)>>? GetTimeSeriesRunner<TResult>(this IActiveSession Session,
            Int32 RunnerNumber,
            HttpContext Context)
        {
            return Session.GetRunner<IEnumerable<(DateTime, TResult)>>(RunnerNumber, Context);
        }

    }
}
