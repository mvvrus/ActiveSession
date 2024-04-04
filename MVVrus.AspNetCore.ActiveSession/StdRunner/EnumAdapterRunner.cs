using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using static MVVrus.AspNetCore.ActiveSession.RunnerStatus;
using static MVVrus.AspNetCore.ActiveSession.StdRunner.StdRunnerConstants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MVVrus.AspNetCore.ActiveSession.Internal;

namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// This class serves as an adapter to return data from an enumerable object implementing IEnumerable&lt;<typeparamref name="TItem"/>&gt; interface
    /// The adapter enumerates the enumerable object in background and returns parts of resulting sequence in order
    /// via <see cref="IRunner{TResult}"/> interface with TResult being <see cref="IEnumerable{TItem}"/>
    /// </summary>
    public class EnumAdapterRunner<TItem> : EnumerableRunnerBase<TItem>, ICriticalNotifyCompletion
    {
        //TODO Implement logging

        IEnumerable<TItem>? _source;
        Boolean _passSourceOwnership;
        Task? _enumTask;
        readonly Action _queueAwaitContinuationDelegate;
        internal Task? EnumTask { get => _enumTask; }
        Task? _fetchTask;

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public EnumAdapterRunner(IEnumerable<TItem> Source, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options,
            ILoggerFactory? LoggerFactory) :
            this(Source,true,null,true,null,null,false,RunnerId, Options, LoggerFactory) { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public EnumAdapterRunner(EnumAdapterParams<TItem> Params, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory) :
            this(Params.Source, Params.PassSourceOnership, Params.CompletionTokenSource, Params.PassCtsOwnership, 
                Params.DefaultAdvance, Params.EnumAheadLimit, Params.StartInConstructor, RunnerId, Options, LoggerFactory) {}

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="PassSourceOnership"></param>
        /// <param name="CompletionTokenSource"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <param name="DefaultAdvance"></param>
        /// <param name="EnumAheadLimit"></param>
        /// <param name="StartInConstructor"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        /// <exception cref="ArgumentNullException"></exception>
        protected EnumAdapterRunner(
            IEnumerable<TItem> Source,
            Boolean PassSourceOnership,
            CancellationTokenSource? CompletionTokenSource,
            Boolean PassCtsOwnership,
            Int32? DefaultAdvance,
            Int32? EnumAheadLimit,
            Boolean StartInConstructor,
            RunnerId RunnerId,
            IOptionsSnapshot<ActiveSessionOptions> Options,
            ILoggerFactory? LoggerFactory) :
            base(CompletionTokenSource, PassCtsOwnership, RunnerId,
                LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(typeof(EnumAdapterRunner<TItem>))),
                Options, DefaultAdvance, EnumAheadLimit)
        {
            _source = Source ?? throw new ArgumentNullException(nameof(Source));
            _passSourceOwnership = PassSourceOnership;
            #if TRACE
            Logger?.LogTraceEnumAdapterConstructorEnter(Id);
            #endif
            if(Logger?.IsEnabled(LogLevel.Debug)??false) {
                Logger!.LogDebugEnumAdapterRunnerConstructor(
                    Id,
                    PassSourceOnership,
                    CompletionTokenSource!=null,
                    PassCtsOwnership, 
                    DefaultAdvance ?? Options.Value.DefaultEnumerableAdvance,
                    EnumAheadLimit ?? Options.Value.DefaultEnumerableQueueSize,
                    StartInConstructor);
            }
            _runAwaitContinuationDelegate = RunAwaitContinuation;
            _queueAwaitContinuationDelegate = QueueAwaitContinuationForRunning;
            if (StartInConstructor) this.StartRunning();
            #if TRACE
            Logger?.LogTraceEnumAdapterConstructorExit(Id);
            #endif
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        protected async override Task DisposeAsyncCore()
        {
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerDisposeCore(Id);
            #endif
            if(_fetchTask != null) try {
                    await _fetchTask!;
                }
                catch(Exception e) {
                    if(!(e is TaskCanceledException) && !(e is ObjectDisposedException)) {
                        Logger?.LogErrorEnumAdapterRunnerDisposeException(e,Id);
                    }
                }
            if(_enumTask != null) try {
                    await _enumTask!;
                }
                catch(Exception e) {
                    if(!(e is TaskCanceledException) && !(e is ObjectDisposedException)) {
                        Logger?.LogErrorEnumAdapterRunnerDisposeException(e,Id);
                    }
                }
            await base.DisposeAsyncCore();
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected override void PreDispose()
        {
            base.PreDispose();
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerPreDispose(Id);
            #endif
            QueueAwaitContinuationForRunning();
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerPreDisposeExit(Id);
            #endif
        }


        void ReleaseSource()
        {
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerReleaseSource(Id);
            #endif
            IDisposable? base_disposable = _passSourceOwnership ? Interlocked.Exchange(ref _source, null) as IDisposable : null;
            base_disposable?.Dispose();
            if(base_disposable != null) {
                #if TRACE
                Logger?.LogTraceEnumAdapterRunnerSourceDisposed(Id);
                #endif
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected internal override void StartBackgroundExecution()
        {
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerStartBackgroundEnter(Id);
            #endif
            _enumTask = Task.Run(EnumerateSource);
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerStartBackgroundExit(Id);
            #endif
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="MaxAdvance"></param>
        /// <param name="Result"></param>
        /// <param name="Token"></param>
        /// <param name="TraceIdentifier"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        protected internal override Task FetchRequiredAsync(Int32 MaxAdvance, List<TItem> Result, CancellationToken Token, String TraceIdentifier)
        {
            _fetchTask=FetchRequiredAsyncImpl(MaxAdvance,Result,Token, TraceIdentifier);
            return _fetchTask;
        }

        async Task FetchRequiredAsyncImpl(Int32 MaxAdvance, List<TItem> Result, CancellationToken Token, String TraceIdentifier)
        {
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerFetchRequiredAsyncEnter(Id, TraceIdentifier);
            #endif
            using(Token.Register(_queueAwaitContinuationDelegate)) {
                #if TRACE
                Logger?.LogTraceEnumAdapterRunnerFetchRequiredAsyncLoopStart(Id, TraceIdentifier);
                #endif
                while(!Disposed() && Result.Count < MaxAdvance && Status.IsRunning() ){
                    #if TRACE
                    Logger?.LogTraceEnumAdapterRunnerFetchRequiredAsyncLoopNext(Id, TraceIdentifier);
                    #endif
                    Token.ThrowIfCancellationRequested();

                    TItem? item;
                    if(QueueTryTake(out item)) {
                        #if TRACE
                        Logger?.LogTraceEnumAdapterRunnerFetchRequiredAsyncItemTaken(Id, TraceIdentifier);
                        #endif
                        Result.Add(item!);
                    }
                    else if(QueueIsAddingCompleted) {
                        #if TRACE
                        Logger?.LogTraceEnumAdapterRunnerFetchRequiredAsyncNoMoreItems(Id, TraceIdentifier);
                        #endif
                        break;
                    }
                    else {
                        #if TRACE
                        Logger?.LogTraceEnumAdapterRunnerFetchRequiredAsyncBeforeAwaiting(Id, TraceIdentifier);
                        #endif
                        await this;
                        #if TRACE
                        Logger?.LogTraceEnumAdapterRunnerFetchRequiredAsyncAfterAwaiting(Id, TraceIdentifier);
                        #endif
                        CheckDisposed();
                    }
                }

            }
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerFetchRequiredAsyncExit(Id, TraceIdentifier);
            #endif
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected override void DoAbort(String TraceIdentifier)
        {
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerDoAbort(Id, TraceIdentifier);
            #endif
            QueueAwaitContinuationForRunning();
            base.DoAbort(TraceIdentifier);
        }

        void EnumerateSource()
        {
            CancellationToken completion_token = CompletionToken;
            if (_source == null) return;
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerEnumerateSourceStart(Id);
            #endif
            try {
                foreach(TItem item in _source!) {
                    #if TRACE
                    Logger?.LogTraceEnumAdapterRunnerEnumerateSourceNewIteration(Id);
                    #endif              
                    if(completion_token.IsCancellationRequested || Status.IsFinal()) {
                        //No need to proceed.
                        #if TRACE
                        Logger?.LogTraceEnumAdapterRunnerEnumerateSourceIterationBreak(Id);
                        #endif
                        break;
                    }
                    if (QueueTryAdd(item, -1, completion_token))
                    {
                        #if TRACE
                        Logger?.LogTraceEnumAdapterRunnerEnumerateSourceItemAdded(Id);
                        #endif
                        QueueAwaitContinuationForRunning();
                    }
                    else if (completion_token.IsCancellationRequested) {
                        //Apparently somewhat excessive check. It's intended to sped up a cancellation
                        //by avoiding one more (possibly long) enumeration at the start of the cycle
                        #if TRACE
                        Logger?.LogTraceEnumAdapterRunnerEnumerateSourceCanceledAfterIteration(Id);
                        #endif
                        break;
                    }
                }
                #if TRACE
                Logger?.LogTraceEnumAdapterRunnerEnumerateSourceIterationExit(Id);
                #endif
            }
            catch (Exception e)
            {
                Logger?.LogErrorEnumAdapterRunnerSourceEnumerationException(e, Id);

                Exception = e;
            }
            finally
            {
                #if TRACE
                Logger?.LogTraceEnumAdapterRunnerEnumerateSourceFinalize(Id);
                #endif
                QueueCompleteAdding();
                ReleaseSource();
                QueueAwaitContinuationForRunning();
            }
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerEnumerateSourceExit(Id);
            #endif
        }

        #region Await stuff
        readonly Action<Action> _runAwaitContinuationDelegate;
        Action? _continuation;
        readonly ManualResetEventSlim _complete_event = new ManualResetEventSlim(true);

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        public EnumAdapterRunner<TItem> GetAwaiter() { return this; }
        /// <summary>
        /// TODO
        /// </summary>
        public bool IsCompleted { get { return QueueCount > 0; } }
        /// <summary>
        /// TODO
        /// </summary>
        public void GetResult() { _complete_event.Wait(); }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="continuation"></param>
        public void OnCompleted(Action continuation)
        {
            Schedule(continuation);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="continuation"></param>
        public void UnsafeOnCompleted(Action continuation)
        {
            Schedule(continuation);
        }

        private void Schedule(Action continuation)
        {
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerEnumerateScheduleContinuation(Id);
            #endif
            _complete_event.Reset();
            try
            {
                if (Interlocked.CompareExchange(ref _continuation, continuation, null) != null)
                {
                    throw new InvalidOperationException("The schedule operation failed for unknown reason.");
                }
            }
            catch(Exception e)
            {
                _complete_event.Set();
                Logger?.LogErrorEnumAdapterRunnerContinuationException(e, Id);
                throw;
            }
            if (QueueIsAddingCompleted)
            {
                #if TRACE
                Logger?.LogTraceEnumAdapterRunnerSheduleQueueContnuationToRunAsLastChancePossible(Id);
                #endif
                QueueAwaitContinuationForRunning();
            }
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerEnumerateScheduleContinuationDone(Id);
            #endif
        }

        void QueueAwaitContinuationForRunning()
        {
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerQueueContnuationToRun(Id);
            #endif
            Action? continuation = Interlocked.Exchange(ref _continuation, null);
            if (continuation != null)
            {
                #if TRACE
                Logger?.LogTraceEnumAdapterRunnerQueueContnuationToRunReally(Id);
                #endif
                ThreadPool.QueueUserWorkItem(_runAwaitContinuationDelegate, continuation, false);
            }
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerQueueContnuationToRunExit(Id);
            #endif
        }

        void RunAwaitContinuation(Action Continuation)
        {
            _complete_event.Set();
            Continuation();
        }
        #endregion
    }
}
