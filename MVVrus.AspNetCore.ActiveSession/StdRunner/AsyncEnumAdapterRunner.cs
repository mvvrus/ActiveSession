using Microsoft.Extensions.Options;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using static MVVrus.AspNetCore.ActiveSession.RunnerStatus;
using static MVVrus.AspNetCore.ActiveSession.StdRunner.StdRunnerConstants;


namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public class AsyncEnumAdapterRunner<TItem> : EnumerableRunnerBase<TItem>
    {
        readonly Action<Task<bool>> _itemActionDelegate;

        readonly IAsyncEnumerable<TItem> _asyncSource;
        readonly bool _asyncEnumerableOwned; 

        IAsyncEnumerator<TItem> _asyncEnumerator = null!;
        volatile FetchContext? _fetchContext;
        volatile Task _taskChainTail;

        internal Task EnumTask { get => _taskChainTail; } //For tests only

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="AsyncSource"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public AsyncEnumAdapterRunner(IAsyncEnumerable<TItem> AsyncSource, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory) :
            this(AsyncSource, true, null, true, null, null, false, RunnerId, Options, LoggerFactory) { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public AsyncEnumAdapterRunner(AsyncEnumAdapterParams<TItem> Params, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory): 
            this(Params.Source,Params.PassSourceOnership,Params.CompletionTokenSource,Params.PassCtsOwnership,
                Params.DefaultAdvance,Params.EnumAheadLimit, Params.StartInConstructor, RunnerId, Options, LoggerFactory) { }

        AsyncEnumAdapterRunner(
            IAsyncEnumerable<TItem> AsyncSource,
            Boolean PassSourceOnership,
            CancellationTokenSource? CompletionTokenSource,
            Boolean PassCtsOwnership,
            Int32? DefaultAdvance,
            Int32? EnumAheadLimit,
            Boolean StartInConstructor,
            RunnerId RunnerId,
            IOptionsSnapshot<ActiveSessionOptions> Options,
            ILoggerFactory? LoggerFactory):
            this(AsyncSource,PassSourceOnership,CompletionTokenSource,PassCtsOwnership,DefaultAdvance,EnumAheadLimit, 
                StartInConstructor, RunnerId, Options,
                LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(typeof(AsyncEnumAdapterRunner<TItem>))))  { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="AsyncSource"></param>
        /// <param name="PassSourceOnership"></param>
        /// <param name="CompletionTokenSource"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <param name="DefaultAdvance"></param>
        /// <param name="EnumAheadLimit"></param>
        /// <param name="StartInConstructor"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="Logger"></param>
        protected AsyncEnumAdapterRunner(
            IAsyncEnumerable<TItem> AsyncSource,
            Boolean PassSourceOnership,
            CancellationTokenSource? CompletionTokenSource,
            Boolean PassCtsOwnership,
            Int32? DefaultAdvance,
            Int32? EnumAheadLimit,
            Boolean StartInConstructor,
            RunnerId RunnerId,
            IOptionsSnapshot<ActiveSessionOptions> Options,
            ILogger? Logger) : base(CompletionTokenSource, PassCtsOwnership, RunnerId, Logger, Options, DefaultAdvance, EnumAheadLimit)
        {
            #if TRACE
            Logger?.LogTraceAsyncEnumAdapterConstructorEnter(Id);
            #endif
            if(Logger?.IsEnabled(LogLevel.Debug) ?? false) {
                Logger!.LogDebugAsyncEnumAdapterRunnerConstructor(
                    Id,
                    PassSourceOnership,
                    CompletionTokenSource != null,
                    PassCtsOwnership,
                    DefaultAdvance ?? Options.Value.DefaultEnumerableAdvance,
                    EnumAheadLimit ?? Options.Value.DefaultEnumerableQueueSize,
                    StartInConstructor);
            }
            _asyncSource = AsyncSource;
            _taskChainTail = Task.CompletedTask;
            _itemActionDelegate = ItemAction;
            _asyncEnumerableOwned=PassSourceOnership;
             if(StartInConstructor) this.StartRunning();
            #if TRACE
            Logger?.LogTraceAsyncEnumAdapterConstructorExit(Id);
            #endif
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected override void PreDispose()
        {
            base.PreDispose();
            #if TRACE
            Logger?.LogTraceAsyncEnumAdapterPreDispose(Id);
            #endif
            FetchContext? released_context = TryReleaseFetchContext();
            if(released_context!=null) {
                #if TRACE
                Logger?.LogTraceAsyncEnumAdapterRunnerFetchRequiredAsyncFailAsDisposed(Id, released_context.TraceIdentifier);
                #endif
                released_context.ResultTaskSource.TrySetException(new ObjectDisposedException(DisposedObjectName()));
            }
            #if TRACE
            Logger?.LogTraceAsyncEnumAdapterRunnerPreDisposeExit(Id);
            #endif
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        protected override async Task DisposeAsyncCore()
        {
            Task task_chain_tail;
            #if TRACE
            Logger?.LogTraceAsyncEnumAdapterRunnerDisposeCore(Id);
            #endif
            DoAbort(UNKNOWN_TRACE_IDENTIFIER);
            do
            {
                task_chain_tail = _taskChainTail;
                await task_chain_tail;
            } while (_taskChainTail != task_chain_tail);
            if (_asyncEnumerator != null) await _asyncEnumerator.DisposeAsync();

            if (_asyncEnumerableOwned)
            {
                IAsyncDisposable? async_disposable = _asyncSource as IAsyncDisposable;
                if (async_disposable != null) await async_disposable.DisposeAsync();
                else (_asyncSource as IDisposable)?.Dispose();
                #if TRACE
                Logger?.LogTraceAsyncEnumAdapterRunnerSourceDisposed(Id);
                #endif
            }
            await base.DisposeAsyncCore();
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="MaxAdvance"></param>
        /// <param name="Result"></param>
        /// <param name="Token"></param>
        /// <param name="TraceIdentifier"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected internal override Task FetchRequiredAsync(Int32 MaxAdvance, List<TItem> Result, CancellationToken Token, String TraceIdentifier)
        {
            #if TRACE
            Logger?.LogTraceAsyncEnumAdapterRunnerFetchRequiredAsyncEnter(Id, TraceIdentifier);
            #endif
            Debug.Assert(_fetchContext == null);
            FetchContext fetch_context = new FetchContext(MaxAdvance, Result, Token, TraceIdentifier);
            try {
                if(Disposed()) {
                    fetch_context.ResultTaskSource.TrySetException(new ObjectDisposedException(DisposedObjectName()));
                    #if TRACE
                    Logger?.LogTraceAsyncEnumAdapterRunnerFetchRequiredAsyncFailAsDisposed(Id, TraceIdentifier);
                    #endif
                }
                else if(Token.IsCancellationRequested) {
                    fetch_context.ResultTaskSource.TrySetCanceled();
                    #if TRACE
                    Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceCancelFetchTask(Id, TraceIdentifier);
                    #endif
                }
                else {
                    TItem item;
                    #if TRACE
                    Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceCopyFetchedItems(Id, TraceIdentifier);
                    #endif
                    while(!Status.IsFinal() && Result.Count < MaxAdvance && QueueTryTake(out item)) fetch_context.Result.Add(item);
                    if(Result.Count >= MaxAdvance || Status.IsFinal() || QueueIsAddingCompleted) {
                        fetch_context.ResultTaskSource.TrySetResult();
                        #if TRACE
                        Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceCompleteFetchTask(Id, TraceIdentifier);
                        #endif
                    }
                    else {
                        //The fetch cannot be completed right here, store the context of it to be processed after some more background work 
                        _fetchContext = fetch_context;
                        #if TRACE
                        Logger?.LogTraceAsyncEnumAdapterRunnerFetchRequiredAsyncStoreContext(Id, TraceIdentifier);
                        #endif
                    }
                }

            }
            finally {
                if(_fetchContext == null) fetch_context.Dispose();
            }
            #if TRACE
            Logger?.LogTraceAsyncEnumAdapterRunnerFetchRequiredAsyncExit(Id, TraceIdentifier);
            #endif
            return fetch_context.ResultTaskSource.Task;
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected internal override void StartBackgroundExecution()
        {
            #if TRACE
            Logger?.LogTraceAsyncEnumAdapterRunnerStartBackgroundEnter(Id);
            #endif
            //Start _asyncEnumerable enumeration task chain
            _asyncEnumerator = _asyncSource.GetAsyncEnumerator(CompletionToken);
            _taskChainTail=_asyncEnumerator.MoveNextAsync().AsTask()
                .ContinueWith(_itemActionDelegate, TaskContinuationOptions.RunContinuationsAsynchronously);
            #if TRACE
            Logger?.LogTraceAsyncEnumAdapterRunnerStartBackgroundExit(Id);
            #endif
        }

        void ItemAction(Task<bool> NextStep)
        {
            bool proceed = false;
            bool result_ready;
            bool status_is_final;

            try
            {
                #if TRACE
                Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceStepComplete(Id);
                #endif
                if(NextStep.IsCanceled) {
                    #if TRACE
                    Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceStepCanceled(Id);
                    #endif
                    Abort();
                }
                result_ready = status_is_final = Status.IsFinal();
                if (NextStep.IsFaulted) {
                    Exception = (NextStep.Exception as AggregateException)?.InnerExceptions.ElementAtOrDefault(0)?? NextStep.Exception;
                    Logger?.LogErrorAsyncEnumAdapterRunnerSourceEnumerationException(Exception, Id);
                    QueueCompleteAdding();
                    #if TRACE
                    Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceChainBreak(Id);
                    #endif
                }
                else if (NextStep.IsCompletedSuccessfully)  {
                    if(NextStep.Result && !status_is_final) {
                        #if TRACE
                        Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceItemAdded(Id);
                        #endif
                        QueueTryAdd(_asyncEnumerator.Current, -1, default);
                        proceed = true;
                    }
                    else {
                        #if TRACE
                        Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceChainBreak(Id);
                        #endif
                        if(status_is_final) { //The queue may be legally disposed already, if so - eat the exception thrown due to this
                            try { QueueCompleteAdding(); } catch(ObjectDisposedException) { }
                        }
                        else {
                            QueueCompleteAdding();
                        }
                    }
                }
                else { // NextStep.IsCanceled==true here
                    #if TRACE
                    Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceChainBreak(Id);
                    #endif
                    QueueCompleteAdding();
                }
            }
            catch (Exception e)
            {
                Exception = e;
                Logger?.LogErrorAsyncEnumAdapterRunnerSourceEnumerationException(Exception, Id);
                QueueCompleteAdding();
                #if TRACE
                Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceChainBreak(Id);
                #endif
            }

            FetchContext? fetch_context = _fetchContext;
            if (fetch_context != null)
            {
                #if TRACE
                Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceFetchTaskActive(Id, fetch_context.TraceIdentifier);
                #endif
                TItem? item;

                if(fetch_context.Token.IsCancellationRequested) {
                    #if TRACE
                    Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceCancelFetchTask(Id, fetch_context.TraceIdentifier);
                    #endif
                    TryReleaseFetchContext();
                }
                else {
                    #if TRACE
                    Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceCopyFetchedItems(Id, fetch_context.TraceIdentifier);
                    #endif
                    while(fetch_context.Result.Count < fetch_context.MaxAdvance && QueueTryTake(out item)) fetch_context.Result.Add(item);
                    if(QueueIsAddingCompleted || fetch_context.Result.Count >= fetch_context.MaxAdvance) {
                        TryReleaseFetchContext();
                        #if TRACE
                        Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceCompleteFetchTask(Id, fetch_context.TraceIdentifier);
                        #endif
                        fetch_context.ResultTaskSource.TrySetResult();
                    }
                }            
            }

            if(proceed && !Disposed()) {
                #if TRACE
                Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceIterationContnue(Id);
                #endif
                _taskChainTail = _asyncEnumerator.MoveNextAsync().AsTask().ContinueWith(_itemActionDelegate);
            }
            else {
                #if TRACE
                Logger?.LogTraceAsyncEnumAdapterRunnerEnumerateSourceIterationDone(Id);
                #endif
            }
        }

        FetchContext? TryReleaseFetchContext()
        {
            #if TRACE
            Logger?.LogTraceAsyncEnumAdapterRunnerTryReleaseFetchContext(Id);
            #endif
            FetchContext? fetch_context = Interlocked.Exchange(ref _fetchContext, null);
            if(fetch_context!=null) {
                String trace_identifier = fetch_context.TraceIdentifier;
                fetch_context.Dispose();
                #if TRACE
                Logger?.LogTraceAsyncEnumAdapterRunnerFetchContextReleased(Id, trace_identifier);
                #endif
            }
            return fetch_context;
        }

        class FetchContext: IDisposable
        {
            public int MaxAdvance { get; init; }
            public TaskCompletionSource ResultTaskSource { get; init; }
            public List<TItem> Result { get; init; }
            public CancellationToken Token { get; init; }
            public String TraceIdentifier { get; init; }
            CancellationTokenRegistration? _callback=null;


            public FetchContext(int MaxAdvance, List<TItem> Result, CancellationToken Token, String TraceIdentifier)
            {
                ResultTaskSource = new TaskCompletionSource();
                this.MaxAdvance = MaxAdvance;
                this.Result = Result;
                this.Token = Token;
                if(Token.CanBeCanceled) _callback = Token.Register(CancelResultTask);
                this.TraceIdentifier = TraceIdentifier;
            }

            void CancelResultTask()
            {
                ResultTaskSource.TrySetCanceled();
            }

            public void Dispose()
            {
                if(_callback!=null) 
                    lock(this) {
                        _callback?.Dispose();
                        _callback = null;
                    }
            }

        }
    }
}
