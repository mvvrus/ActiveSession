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
    /// This class implements a sequence-oriented runner that is an adapter for an object 
    /// implementing the <see cref="IEnumerable{T}">IEnumerable&lt;<typeparamref name="TItem"/>&gt;</see> interface.
    /// The adapter enumerates this enumerable object in background and returns parts of resulting sequence in order
    /// via <see cref="IRunner{TResult}"/> interface with TResult being <see cref="IEnumerable{TItem}">IEnumerable&lt;TItem&gt;</see>
    /// </summary>
    /// <remarks>
    /// <inheritdoc path="/remarks/seqrunner"/>
    /// </remarks>
    public class EnumAdapterRunner<TItem> : EnumerableRunnerBase<TItem>, ICriticalNotifyCompletion
    {
        IEnumerable<TItem>? _source;
        Boolean _passSourceOwnership;
        Task? _enumTask;
        readonly Action _queueAwaitContinuationDelegate;
        internal override Task? EnumTask { get => _enumTask; }
        Task? _fetchTask;

        /// <summary>
        /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/summary/common' />
        /// <factory>This constructor is used to create an instance by <see cref="TypeRunnerFactory{TRequest, TResult}">TypeRunnerFactory</see></factory>
        /// </summary>
        /// <param name="Source">
        /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Source"]' />
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="RunnerId"]' />
        /// </param>
        /// <param name="Options">
        /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Options"]' />
        /// </param>
        /// <param name="Logger">
        /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Logger"]' />
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public EnumAdapterRunner(IEnumerable<TItem> Source, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options,
            ILogger<EnumAdapterRunner<TItem>>? Logger) :
            this(Source,true,null,true,null,null,false,RunnerId, Options, Logger) { }

        /// <summary>
        /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/summary/common' />
        /// <factory>This constructor is used to create an instance by <see cref="TypeRunnerFactory{TRequest, TResult}">TypeRunnerFactory</see></factory>
        /// </summary>
        /// <param name="Params">A structure that contains a refernce to the source enumerable and additional parameters.</param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="RunnerId"]' />
        /// </param>
        /// <param name="Options">
        /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Options"]' />
        /// </param>
        /// <param name="Logger">
        /// <inheritdoc cref="EnumAdapterRunner{TItem}.EnumAdapterRunner(IEnumerable{TItem}, bool, CancellationTokenSource?, bool, int?, int?, bool, RunnerId, IOptionsSnapshot{ActiveSessionOptions}, ILogger?)" path='/param[@name="Logger"]' />
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public EnumAdapterRunner(EnumAdapterParams<TItem> Params, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, 
            ILogger<EnumAdapterRunner<TItem>>? Logger) :
            this(Params.Source, Params.PassSourceOnership, Params.CompletionTokenSource, Params.PassCtsOwnership, 
                Params.DefaultAdvance, Params.EnumAheadLimit, Params.StartInConstructor, RunnerId, Options, Logger) {}

        /// <summary>
        /// <common>A constructor that creates EnumAdapterRunner instance.</common>
        /// This constructor has protected access level and is intended for use in other constructors of this and descendent classes.
        /// </summary>
        /// <param name="Source">An enumerable for which the instance to be created will serve as an adapter.</param>
        /// <param name="PassSourceOnership">
        /// Flag showing that the instance to be created will be responsible for disposing the <paramref name="Source"/> value passed to it.
        /// </param>
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
        /// <param name="StartInConstructor">Set to <see langword="true"/> to start background processing in the constructor.</param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="RunnerBase.RunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?)" path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Options">
        /// <inheritdoc cref="EnumerableRunnerBase{TItem}.EnumerableRunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?, IOptionsSnapshot{ActiveSessionOptions}, int?, int?)" path='/param[@name="Options"]'/>
        /// </param>
        /// <param name="Logger">
        /// <inheritdoc cref="EnumerableRunnerBase{TItem}.EnumerableRunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?, IOptionsSnapshot{ActiveSessionOptions}, int?, int?)" path='/param[@name="Logger"]'/>
        /// </param>
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
            ILogger? Logger) :
            base(CompletionTokenSource, PassCtsOwnership, RunnerId,
                Logger, Options, DefaultAdvance, EnumAheadLimit)
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
            _queueAwaitContinuationDelegate = () => EnqueueAwaitContinuationForRunning();
            if (StartInConstructor) this.StartRunning();
            #if TRACE
            Logger?.LogTraceEnumAdapterConstructorExit(Id);
            #endif
        }

        /// <summary>
        /// Protected, overrides <see cref="EnumerableRunnerBase{TItem}.DisposeAsyncCore">EnumerableRunnerBase.DisposeAsyncCore</see>.
        /// <inheritdoc path="/summary/toinherit"/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        /// <remarks>
        /// This override first awaits completion of the fetch task used by the implementation of  
        /// <see cref="EnumerableRunnerBase{TItem}.GetRequiredAsync(int, CancellationToken, int, string?)">GetRequiredAsync</see> 
        /// method, if such a task is running.
        /// Then it awaits completion of the background task enumerating the enumerable for which this instance serves an an adapter.
        /// And at last it awaits its base method.
        /// </remarks>
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
        /// Protected, overrides <see cref="EnumerableRunnerBase{TItem}.PreDispose">EnumerableRunnerBase.PreDispose</see>.
        /// <inheritdoc path="/summary/toinherit/text()"/>
        /// </summary>
        /// <remarks>
        /// This override first calls its base method.
        /// Then it resumes the fetch task used by the implementation of <see cref="EnumerableRunnerBase{TItem}.GetRequiredAsync(int, CancellationToken, int, string?)">GetRequiredAsync</see> 
        /// method, if such a task is awaiting continuation, so the task terminates itself due to <see cref="ObjectDisposedException"/> 
        /// thrown (the exception usually not being revealed to a caller).
        /// </remarks>
        protected override void PreDispose()
        {
            base.PreDispose();
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerPreDispose(Id);
            #endif
            EnqueueAwaitContinuationForRunning();
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerPreDisposeExit(Id);
            #endif
        }


        /// <summary>
        /// Protected, overrides <see cref="RunnerBase.StartBackgroundExecution">RunnerBase.StartBackgroundExecution</see> abstract method.
        /// <inheritdoc path='/summary/toinherit'/>
        /// </summary>
        /// <remarks>Starts a background task enumerating the source enumerable for which this instance serves as an adapter.</remarks>
        protected internal override void StartBackgroundExecution()
        {
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerStartBackgroundEnter(Id);
            #endif
            _enumTask = new Task(EnumerateSource, CompletionToken, TaskCreationOptions.LongRunning);
            _enumTask.Start();
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerStartBackgroundExit(Id);
            #endif
        }

        /// <summary>
        /// Protected, overrides <see cref="EnumerableRunnerBase{TItem}.FetchRequiredAsync(int, List{TItem}, CancellationToken, string)">
        /// EnumerableRunnerBase.FetchRequiredAsync</see> abstract method.
        /// <inheritdoc path='/summary/toinherit/node()'/>
        /// </summary>
        /// <param name="MaxAdvance"><inheritdoc path='/param[@name="MaxAdvance"]/node()'/></param>
        /// <param name="Result"><inheritdoc path='/param[@name="Result"]/node()'/></param>
        /// <param name="Token"><inheritdoc path='/param[@name="Token"]/node()'/></param>
        /// <param name="TraceIdentifier"><inheritdoc path='/param[@name="TraceIdentifier"]/node()'/></param>
        /// <returns><inheritdoc/></returns>
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
                while(Result.Count < MaxAdvance && Status.IsRunning() ){
                    #if TRACE
                    Logger?.LogTraceEnumAdapterRunnerFetchRequiredAsyncLoopNext(Id, TraceIdentifier);
                    #endif

                    CheckDisposed();
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
                    }
                }

            }
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerFetchRequiredAsyncExit(Id, TraceIdentifier);
            #endif
        }

        /// <summary>
        /// Protected, overrides <see cref="EnumerableRunnerBase{TItem}.DoAbort(string)"> EnumerableRunnerBase.DoAbort</see> method.
        /// <inheritdoc path='/summary/toinherit/node()'/>
        /// </summary>
        /// <remarks>
        /// This method override first resumes a result fetching task if it is awaiting the background task result
        /// and then it calls the base class method. 
        /// <inheritdoc path='/remarks/toinherit/node()'/>
        /// </remarks>
        /// <inheritdoc/>
        protected override void DoAbort(String TraceIdentifier)
        {
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerDoAbort(Id, TraceIdentifier);
            #endif
            EnqueueAwaitContinuationForRunning();
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
                    if (QueueTryAdd(item))
                    {
                        #if TRACE
                        Logger?.LogTraceEnumAdapterRunnerEnumerateSourceItemAdded(Id);
                        #endif
                        EnqueueAwaitContinuationForRunning();
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
                EnqueueAwaitContinuationForRunning();
            }
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerEnumerateSourceExit(Id);
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

        #region Await stuff
#pragma warning disable IDE0051 // Remove unused private members. These methods are realy used by an await operator implementation.
        EnumAdapterRunner<TItem> GetAwaiter() { return this; }
        bool IsCompleted { get { return QueueCount > 0; } }
        void GetResult() { _complete_event.Wait(); }
#pragma warning restore IDE0051 // Remove unused private members
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action Continuation) { Schedule(Continuation);}
        void INotifyCompletion.OnCompleted(Action Continuation) {Schedule(Continuation, true);}

        readonly Action<Action> _runAwaitContinuationDelegate;
        Action? _continuation;
        readonly ManualResetEventSlim _complete_event = new ManualResetEventSlim(true);

        private void Schedule(Action continuation, Boolean PassExecutionContext=false)
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
                EnqueueAwaitContinuationForRunning(PassExecutionContext);
            }
            #if TRACE
            Logger?.LogTraceEnumAdapterRunnerEnumerateScheduleContinuationDone(Id);
            #endif
        }

        void EnqueueAwaitContinuationForRunning(Boolean PassExecutionContext = false)
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
                if(PassExecutionContext) ThreadPool.QueueUserWorkItem(_runAwaitContinuationDelegate, continuation, false);
                else ThreadPool.UnsafeQueueUserWorkItem(_runAwaitContinuationDelegate, continuation, false);
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
