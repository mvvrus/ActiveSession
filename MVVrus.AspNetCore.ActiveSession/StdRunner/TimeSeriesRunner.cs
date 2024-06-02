using Microsoft.Extensions.Options;
using MVVrus.AspNetCore.ActiveSession.Internal;

namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// Used for implementation of a sequence-oriented runner that returns a time series - 
    /// a sequence of pairs of a measurement time and a measured value (of type TResult) at this time.
    /// </summary>
    /// <typeparam name="TResult">Type of the value to be measured</typeparam>
    /// <remarks>
    /// <para>
    /// This class implements the <see cref="IRunner{TResult}"> IRunner&lt;IEnumerable&lt;(DateTime,TResult)&gt;&gt;</see> interface.
    /// </para>
    /// <para>
    /// The measurements are performed by invoking the specified delegate at the specified points of time. 
    /// </para>
    /// <para>
    /// The measurement process begins from the the first GetRequiredAsync call, or, may be, the runner creation.
    /// Measurements are separated by specified intervals.  For details see parameters of the class constructors.
    /// </para>
    /// <para>
    /// The process of measurement tries to avoid accumulation of inaccuracies in determining measurement moments 
    /// and in a duration of the whole process of obtaining the series
    /// by adjusting the intervals between measurements to compensate delays due to measurement delegate calls.
    /// </para>
    /// </remarks>
    public class TimeSeriesRunner<TResult> : AsyncEnumAdapterRunner<(DateTime, TResult)>
    {
        /// <summary>
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(TimeSeriesParams{TResult}, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/summary' />
        /// Creates a runner producing an unlimited series of measurements.
        /// </summary>
        /// <param name="SeriesParam">
        /// A pair of values. 
        /// The first value is a delegate that returns a result of a measurement. 
        /// The second value is a requested interval between measurements.
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Options">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="Options"]'/>
        /// </param>
        /// <param name="LoggerFactory">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="LoggerFactory"]'/>
        /// </param>
        [ActiveSessionConstructor]
        public TimeSeriesRunner(ValueTuple<Func<TResult>, TimeSpan> SeriesParam, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory) :
            this(SeriesParam.Item1, SeriesParam.Item2, null, null, true, null, null, false, RunnerId, Options, LoggerFactory) { }

        /// <summary>
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(TimeSeriesParams{TResult}, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/summary' />
        /// Creates a runner producing a finite series of measurements.
        /// </summary>
        /// <param name="SeriesParam">
        /// A group of three values. 
        /// The first value is a delegate that returns a result of a measurement. 
        /// The second value is a requested interval between measurements.
        /// The third value is a maximum number of measurements to be performed. 
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Options">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="Options"]'/>
        /// </param>
        /// <param name="LoggerFactory">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="LoggerFactory"]'/>
        /// </param>
        [ActiveSessionConstructor]
        public TimeSeriesRunner(ValueTuple<Func<TResult>, TimeSpan, Int32> SeriesParam, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory) :
            this(SeriesParam.Item1, SeriesParam.Item2, SeriesParam.Item3, null, true, null, null, false, RunnerId, Options, LoggerFactory)
        { }

        /// <summary>
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/summary/common' />
        /// This constructor is used to create an instance via <see cref="TypeRunnerFactory{TRequest, TResult}">TypeRunnerFactory</see>.
        /// </summary>
        /// <param name="SeriesParam">
        /// A structure that contains parameters used for creation of the instance. 
        /// Fields of the structure contains the same values as the paramemters of  the 
        /// <see cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)">
        /// protected constructor
        /// </see> with the same names.
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Options">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="Options"]'/>
        /// </param>
        /// <param name="LoggerFactory">
        /// <inheritdoc cref="TimeSeriesRunner{TResult}.TimeSeriesRunner(Func{TResult}, TimeSpan, int?, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILoggerFactory?)" path='/param[@name="LoggerFactory"]'/>
        /// </param>
        [ActiveSessionConstructor]
        public TimeSeriesRunner(TimeSeriesParams<TResult> SeriesParam, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory) :
            this(SeriesParam.Gauge, SeriesParam.Interval, SeriesParam.Count, SeriesParam.CompletionTokenSource, SeriesParam.PassCtsOwnership, SeriesParam.DefaultAdvance, SeriesParam.EnumAheadLimit, SeriesParam.StartInConstructor, RunnerId, Options, LoggerFactory)
        { }

        /// <summary>
        /// <common>A constructor that creates TimeSeriesRunner instance.</common>
        /// This constructor has protected access level and is intended for use in other constructors of this and descendent classes.
        /// </summary>
        /// <param name="Gauge">A delegate that returns a result of a measurement.</param>
        /// <param name="Interval">Requested interval between measurements.</param>
        /// <param name="Count">Maximum number of measurements in the series. 
        /// May be null to produce a series with unlimited number of measurements.</param>
        /// <param name="CompletionTokenSource">
        /// <inheritdoc cref="RunnerBase.RunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?)" path='/param[@name="CompletionTokenSource"]'/>
        /// </param>
        /// <param name="PassCtsOwnership">
        /// <inheritdoc cref="RunnerBase.RunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?)" path='/param[@name="PassCtsOwnership"]'/>
        /// </param>
        /// <param name="DefaultAdvance">
        /// <inheritdoc cref="EnumerableRunnerBase{TItem}.EnumerableRunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?, IOptionsSnapshot{ActiveSessionOptions}, int?, int?)" path='/param[@name="DefaultAdvance"]'/>
        /// </param>
        /// <param name="EnumAheadLimit">
        /// <inheritdoc cref="EnumerableRunnerBase{TItem}.EnumerableRunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?, IOptionsSnapshot{ActiveSessionOptions}, int?, int?)" path='/param[@name="QueueSize"]'/>
        /// </param>
        /// <param name="StartInConstructor">
        /// <inheritdoc cref="AsyncEnumAdapterRunner{TItem}.AsyncEnumAdapterRunner(IAsyncEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="StartInConstructor"]'/>
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="RunnerBase.RunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?)" path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Options">
        /// <inheritdoc cref="EnumerableRunnerBase{TItem}.EnumerableRunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?, IOptionsSnapshot{ActiveSessionOptions}, int?, int?)" path='/param[@name="Options"]'/>
        /// </param>
        /// <param name="LoggerFactory">A logger factory used to create a logger for the instance to be created (usually it is taken from DI container)</param>
        protected TimeSeriesRunner(Func<TResult> Gauge, 
            TimeSpan Interval, 
            Int32? Count, 
            CancellationTokenSource? CompletionTokenSource, 
            Boolean PassCtsOwnership, 
            Int32? DefaultAdvance, 
            Int32? EnumAheadLimit, 
            Boolean StartInConstructor, 
            RunnerId RunnerId, 
            IOptionsSnapshot<ActiveSessionOptions> Options, 
            ILoggerFactory? LoggerFactory)
            : base(new TimeSeriesAsyncEnumerable(Gauge,Interval,Count), true, CompletionTokenSource, PassCtsOwnership, DefaultAdvance, EnumAheadLimit, StartInConstructor, RunnerId, Options,
                LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(typeof(TimeSeriesRunner<ValueTuple<Func<TResult>, TimeSpan>>))))
        {
            Logger?.LogDebugTimeSeriesRunnerConstructor(RunnerId, Interval, Count);
        }

        internal class TimeSeriesAsyncEnumerable : IAsyncEnumerable<(DateTime, TResult)>
        {
            readonly Func<TResult> _gauge;
            readonly TimeSpan _interval;
            readonly Int32? _count;

            public TimeSeriesAsyncEnumerable(Func<TResult> Gauge, TimeSpan Interval, Int32? Count)
            {
                _gauge = Gauge ?? throw new ArgumentNullException(nameof(Gauge));
                _interval = Interval > TimeSpan.Zero ? Interval : throw new ArgumentOutOfRangeException(nameof(Interval));
                _count = (Count == null || Count > 0) ? Count : throw new ArgumentOutOfRangeException(nameof(Count));
            }

            public IAsyncEnumerator<(DateTime, TResult)> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new TimeSeriesAsyncEnumerator(_gauge, _interval, _count, cancellationToken);
            }

        }

        class TimeSeriesAsyncEnumerator : IAsyncEnumerator<(DateTime, TResult)>
        {
            readonly Func<TResult> _gauge;
            readonly long _interval;
            readonly Int32? _count;
            readonly long _startTime;
            CancellationTokenSource? _disposeCts = null;
            CancellationTokenSource? _delayCts = null;
            CancellationToken _delay_token = default;
            Int32 _index = 0;
            Task? _enumTask = null;
            Int32 _disposing = 0;
            Task? _disposeTask = null;

            public TimeSeriesAsyncEnumerator(Func<TResult> Gauge, TimeSpan Interval, Int32? Count, CancellationToken cancellationToken)
            {
                _gauge = Gauge ?? throw new ArgumentNullException(nameof(Gauge));
                _interval = Interval > TimeSpan.Zero ? Interval.Ticks : throw new ArgumentOutOfRangeException(nameof(Interval));
                _count = (Count == null || Count >0) ? Count : throw new ArgumentOutOfRangeException(nameof(Count));
                _startTime = DateTime.Now.Ticks;
                _delay_token = cancellationToken;
                _disposeCts = new CancellationTokenSource();
                if(cancellationToken.CanBeCanceled) {
                    _delayCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);
                    _delay_token = _delayCts.Token;
                }
            }


            public (DateTime, TResult) Current { get; private set; } = default;

            public async ValueTask<Boolean> MoveNextAsync()
            {
                if(Volatile.Read(ref _disposing) != 0) throw new ObjectDisposedException(nameof(TimeSeriesAsyncEnumerator));
                _delay_token.ThrowIfCancellationRequested();
                if(_index > 0) {
                    if(_count != null && _index >= _count!) {
                        Current = default;
                        return false;
                    }
                    Int32 delay_msec = (Int32)(Math.Max((_startTime + _interval * _index - DateTime.Now.Ticks), 0) / TimeSpan.TicksPerMillisecond);
                    _enumTask = Task.Delay(delay_msec, _delay_token);
                    await _enumTask!;
                }
                _enumTask = null;
                Current = (DateTime.Now, _gauge());
                _index++;
                return true;
            }

            public ValueTask DisposeAsync()
            {
                if(_disposeTask == null) {
                    if(Interlocked.Exchange(ref _disposing, 1) == 0) {
                        _disposeTask = DoDispose();
                    }
                }
                return new ValueTask(_disposeTask!);
            }

            async Task DoDispose()
            {
                _disposeCts?.Cancel();
                Task? enum_task = Interlocked.Exchange(ref _enumTask, null);
                if(enum_task != null) try {
                        await enum_task!;
                    }
                    catch { }
                _delayCts?.Dispose();
                _disposeCts?.Dispose();
            }

        }

    }
}
