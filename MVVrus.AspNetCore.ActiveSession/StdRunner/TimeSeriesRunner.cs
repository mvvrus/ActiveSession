using Microsoft.Extensions.Options;
using MVVrus.AspNetCore.ActiveSession.Internal;

namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <typeparam name="TResult">TODO</typeparam>
    public class TimeSeriesRunner<TResult> : AsyncEnumAdapterRunner<(DateTime, TResult)>
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="SeriesParam"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public TimeSeriesRunner(ValueTuple<Func<TResult>, TimeSpan> SeriesParam, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory) :
            this(SeriesParam.Item1, SeriesParam.Item2, null, null, true, null, null, false, RunnerId, Options, LoggerFactory) { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="SeriesParam"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public TimeSeriesRunner(ValueTuple<Func<TResult>, TimeSpan, Int32> SeriesParam, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory) :
            this(SeriesParam.Item1, SeriesParam.Item2, SeriesParam.Item3, null, true, null, null, false, RunnerId, Options, LoggerFactory)
        { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="SeriesParam"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public TimeSeriesRunner(TimeSeriesParams<TResult> SeriesParam, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory) :
            this(SeriesParam.Gauge, SeriesParam.Interval, SeriesParam.Count, SeriesParam.CompletionTokenSource, SeriesParam.PassCtsOwnership, SeriesParam.DefaultAdvance, SeriesParam.EnumAheadLimit, SeriesParam.StartInConstructor, RunnerId, Options, LoggerFactory)
        { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Gauge"></param>
        /// <param name="Interval"></param>
        /// <param name="Count"></param>
        /// <param name="CompletionTokenSource"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <param name="DefaultAdvance"></param>
        /// <param name="EnumAheadLimit"></param>
        /// <param name="StartInConstructor"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        protected TimeSeriesRunner(Func<TResult> Gauge, TimeSpan Interval, Int32? Count, CancellationTokenSource? CompletionTokenSource, Boolean PassCtsOwnership, Int32? DefaultAdvance, Int32? EnumAheadLimit, Boolean StartInConstructor, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory) : 
            base(new TimeSeriesAsyncEnumerable(Gauge,Interval,Count), true, CompletionTokenSource, PassCtsOwnership, DefaultAdvance, EnumAheadLimit, StartInConstructor, RunnerId, Options,
                LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(typeof(TimeSeriesRunner<ValueTuple<Func<TResult>, TimeSpan>>))))
        {
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
