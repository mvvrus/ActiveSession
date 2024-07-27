using System.Collections.Concurrent;
using static MVVrus.AspNetCore.ActiveSession.RunnerStatus;
using static MVVrus.AspNetCore.ActiveSession.IRunner;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using System.Collections;
using MVVrus.AspNetCore.ActiveSession.Internal;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// This class is a base class for sequence-oriented runner classes (see Remarks section).
    /// It is an abstract class intended to be a base for specific sequence-oriented runner classes.
    /// It implements a common logic for those runners. 
    /// </summary>
    /// <typeparam name="TItem">Type of items of a sequence (<see cref="IEnumerable{T}">IEnumerable&lt;TItem&gt;</see> interface) that a background process of the runner returns.</typeparam>
    /// <remarks>
    /// <seqrunner>
    /// <para>
    /// Sequence-oriented runners are ones that return a sequence of data records as a result.
    /// These runners implements  <see cref="IRunner{TResult}"/> interface, with TResult type being an implementation 
    /// of <see cref="IEnumerable{T}">IEnumerable&lt;TItem&gt;</see> interface.
    /// </para>
    /// <para>
    /// Sequence-oriented runners returns parts of a sequence produced in a background by some process.
    /// The parts are returned via calls of 
    /// <see cref="EnumerableRunnerBase{TItem}.GetRequiredAsync(int, CancellationToken, int, string?)">GetRequiredAsync</see> and/or 
    /// <see cref="EnumerableRunnerBase{TItem}.GetAvailable(int, int, string?)">GetAvailable </see> methods. 
    /// These parts may be obtained during processing of different HTTP requests belonging to one <see cref="IActiveSession">Active Session</see>.
    /// All calls to these methods must be made in order and calls of these methods must not be made in parallel.
    /// </para>
    /// </seqrunner>
    /// <para>
    /// The common logic implemented in this class uses a queue that allows storing in it data fetched in background.
    /// Methods defined by <see cref="IRunner{TResult}"/> interface that returns results
    /// (namely <see cref="GetAvailable(int, int, string?)">GetAvailable</see> and 
    /// <see cref="GetRequiredAsync(int, CancellationToken, int, string?)">GetRequiredAsync</see>)
    /// returns parts of the queue contents in the same order as those items have been placed into the queue.
    /// </para>
    /// <para>
    /// Methods that returns results
    /// (namely <see cref="GetAvailable(int, int, string?)">GetAvailable</see> and 
    /// <see cref="GetRequiredAsync(int, CancellationToken, int, string?)">GetRequiredAsync</see>) 
    /// can start fetching data only from the current <see cref="IRunner.Position"/> of the runner 
    /// overwise <see cref="InvalidOperationException"/> exception will be thrown.
    /// </para>
    /// </remarks>
    public abstract class EnumerableRunnerBase<TItem> : RunnerBase, IRunner<IEnumerable<TItem>>, IAsyncDisposable
    {
        const string PARALLELISM_NOT_ALLOWED = "Parallel operations are not allowed.";

        readonly internal BlockingCollection<TItem> _queue;
        readonly int _defaultAdvance;
        Task? _disposeTask = null;
        List<TItem>? _stashedFetch = null;
        volatile TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>? _waitingTaskSource = null;
        Int32 _queueAddedCount=0;
        //A field used to set pseudo-lock on the runner to block parallel execution of its GetRequiredAsync/GetAvailable methods,
        //The code using this pseudo-lock does not wait but throws an exception then the pseudo-lock cannot be acquired
        int _busy;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
        /// <param name="Options">Presents access to a configuration object via the options pattern</param>
        /// <remarks>
        /// This constructor overload accepts default values for the <paramref name="DefaultAdvance"/> and <paramref name="QueueSize"/> parameters 
        /// from the configuration <see cref="ActiveSessionOptions"/> object passed via <paramref name="Options"/> using the options pattern
        /// </remarks>
        /// <inheritdoc cref="EnumerableRunnerBase{TItem}.EnumerableRunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?, int, int)"/>
        protected EnumerableRunnerBase(
            CancellationTokenSource? CompletionTokenSource, Boolean PassCtsOwnership, RunnerId RunnerId, ILogger? Logger,
            IOptionsSnapshot<ActiveSessionOptions> Options, Int32? DefaultAdvance = null, Int32? QueueSize = null
        ) : this(CompletionTokenSource, PassCtsOwnership, RunnerId, Logger,
                DefaultAdvance ?? Options.Value.DefaultEnumerableAdvance, QueueSize ?? Options.Value.DefaultEnumerableQueueSize) { }

        /// <summary>
        /// Constructor for a runner object to be used in descendent classes
        /// </summary>
        /// <param name="DefaultAdvance">
        /// Default value for the first parameter (Advance) for 
        /// <see cref="EnumerableRunnerBase{TItem}.GetRequiredAsync(int, CancellationToken, int, string?)">GetRequiredAsync</see>
        /// method of the instance to be created.
        /// </param>
        /// <param name="QueueSize">Maximum number of items fetched in background ahead of time in the instance to be created.
        /// </param>
        /// <remarks>
        /// This constructor overload does not accept any default values 
        /// for the <paramref name="DefaultAdvance"/> and <paramref name="QueueSize"/> parameters 
        /// </remarks>
        /// <inheritdoc cref="RunnerBase.RunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?)"/>
        protected EnumerableRunnerBase(
            CancellationTokenSource? CompletionTokenSource, Boolean PassCtsOwnership, RunnerId RunnerId, ILogger? Logger,
            Int32 DefaultAdvance, Int32 QueueSize
        ) : base(CompletionTokenSource, PassCtsOwnership, RunnerId, Logger)
        {
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseConstructorEnter(Id);
            #endif
            _queue = new BlockingCollection<TItem>(QueueSize);
            _defaultAdvance = DefaultAdvance;
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseConstructorExit(Id);
            #endif
        }

        ///<summary>
        ///<inheritdoc/>
        ///This method is a part of implementation of <see cref="IAsyncDisposable"/> interface.
        ///</summary>
        /// <returns>
        /// A <see cref="ValueTask"/> that presents asynchronous process of disposing the runner 
        /// or a completed ValueTask if the process has been already completed.
        /// </returns>
        public ValueTask DisposeAsync()
        {
            if(SetDisposed()) {
                #if TRACE
                Logger?.LogTraceEnumerableRunnerBaseDisposeAsyncExecuted(Id);
                #endif
                _disposeTask = DisposeAsyncCore();
            }
            return _disposeTask!.IsCompleted ? ValueTask.CompletedTask : new ValueTask(_disposeTask!);
        }

        ///<summary>
        ///<inheritdoc path="/summary/toinherit/node()"/>
        ///Overrides <see cref="RunnerBase.Status">RunnerBase.Status</see>.
        ///</summary>
        ///<remarks>
        ///<inheritdoc path="/remarks/toinherit/node()"/>
        ///From a base class (<see cref="RunnerBase"/>) perspective the value of the property at any running stages 
        ///(namely <see cref="RunnerStatus.Stalled"/> or <see cref="RunnerStatus.Progressed"/>) 
        ///always contains the same value: <see cref="RunnerStatus.Stalled"/>. 
        ///But this property override takes also in account the presence of items fetched into its queue,
        ///changing the returned property value to <see cref="RunnerStatus.Progressed"/> if the queue is not empty.
        ///</remarks>
        ///<inheritdoc/>
        public override RunnerStatus Status
        {
            get
            {
                RunnerStatus status = base.Status;
                if(!Disposed() && status == Stalled && (_queue.Count > 0 || _stashedFetch != null)) status = Progressed;
                return status;
            }
        }

        /// <summary>
        /// <inheritdoc path="/summary/toinherit/node()"/>  
        /// Overrides <see cref="RunnerBase.Position">RunnerBase.Position</see>. 
        /// </summary>
        /// <remarks> For sequence-oriente runners implemented by descendants of this class Position designates 
        /// a number of items in sequences returned by all previously completed  
        /// <see cref="GetAvailable">GetAvailable</see> and <see cref="GetRequiredAsync(int, CancellationToken, int, string?)">GetRequiredAsync</see> calls.
        /// </remarks>
        public override Int32 Position { get => base.Position; protected set=>base.Position=value ; }

        /// <summary>
        /// <inheritdoc path="/summary/toinherit" />
        /// This is a part of <see cref="IRunner{TResult}">IRunner&lt;IEnumerable&lt;TItem&gt;&gt;</see> implementation.
        /// </summary>
        /// <param name="Advance">
        /// <common>Maximum number of items in the sequience returned in result's <see cref="RunnerResult{TResult}.Result"/> field.</common>
        /// The default value of the parameter means that all already fetched items should be returned.
        /// </param>
        /// <param name="StartPosition">
        /// <inheritdoc path='/param[@name="StartPosition"]/toinherit' /> 
        /// Must be equal to the current runner's <see cref="Position"/> or a constant <see cref="CURRENT_POSITION"/>
        /// </param>
        /// <inheritdoc path='/param[@name="TraceIdentifier"]' />
        /// <returns>
        /// <inheritdoc path='/returns/toinherit'/>
        /// <common>
        /// the <see cref="RunnerResult{TResult}.Result">Result</see> field of the returned structure contains sequence 
        /// of items (of type TItem) already fetched in background, that have not been returned yet by previous 
        /// <see cref="GetAvailable">GetAvailable</see> and <see cref="GetRequiredAsync(int, CancellationToken, int, string?)">GetRequiredAsync</see> calls 
        /// </common>
        /// - all or a part of them according to the <paramref name="Advance"/> value. 
        /// </returns>
        public RunnerResult<IEnumerable<TItem>> GetAvailable(Int32 Advance = int.MaxValue, Int32 StartPosition = -1, String? TraceIdentifier = null)
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier ?? UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseGetAvailable(Id, trace_identifier);
            #endif
            if(!TryAcquirePseudoLock()) {
                Logger?.LogWarningEnumerableRunnerBaseParallelAttempt(Id, trace_identifier);
                ThrowInvalidParallelism();
            }
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBasePseudoLockAcquired(Id, trace_identifier);
            #endif
            List<TItem> result = new List<TItem>();
            try {
                ProcessEnumParmeters(ref StartPosition, ref Advance, _defaultAdvance, nameof(GetAvailable), trace_identifier, Logger);
                FetchAvailable(Advance, result, trace_identifier);
            }
            catch(Exception exception) {
                Logger?.LogErrorEnumerableRunnerBaseGetAvailException(exception, Id, trace_identifier);
                ReleasePseudoLock();
                #if TRACE
                Logger?.LogTraceEnumerableRunnerBasePseudoLockReleased(Id, trace_identifier);
                #endif
                throw;
            }
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseGetAvailableExit(Id, trace_identifier);
            #endif
            return FinishWithResult(result, trace_identifier);
        }

        /// <summary>
        /// <inheritdoc path="/summary/toinherit" />
        /// This is a part of <see cref="IRunner{TResult}">IRunner&lt;IEnumerable&lt;TItem&gt;&gt;</see> implementation.
        /// </summary>
        /// <param name="Advance">
        /// <inheritdoc cref="GetAvailable(int, int, string?)" path='/param[@name="Advance"]/common' />
        /// The default value of the parameter is substituted by a DefaultAdvance value passed via the constructor.
        /// </param>
        /// <inheritdoc path='/param[@name="Token"]' />
        /// <param name="StartPosition">
        /// <inheritdoc path='/param[@name="StartPosition"]/toinherit' /> 
        /// Must be equal to the current runner's <see cref="Position"/> or a constant <see cref="CURRENT_POSITION"/>
        /// </param>
        /// <inheritdoc path='/param[@name="TraceIdentifier"]' />
        /// <returns>
        /// <inheritdoc path='/returns/toinherit'/>
        /// <inheritdoc cref="GetAvailable(int, int, string?)" path='/returns/common' />. 
        ///  A number of items in the sequence cannot be greater than <paramref name="Advance"/> value, 
        ///  but that number can be less if it is the last part of a sequence produced by a completed background execution. 
        /// </returns>
        public ValueTask<RunnerResult<IEnumerable<TItem>>> GetRequiredAsync(Int32 Advance = 0, CancellationToken Token = default, Int32 StartPosition = -1, String? TraceIdentifier = null)
        {
            CheckDisposed();
            String trace_identifier = TraceIdentifier ?? UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseGetRequired(Id, trace_identifier);
            #endif
            if(!TryAcquirePseudoLock()) {
                Logger?.LogWarningEnumerableRunnerBaseParallelAttempt(Id, trace_identifier);
                ThrowInvalidParallelism();
            }
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBasePseudoLockAcquired(Id, trace_identifier);
            #endif
            try {
                RunnerResult<IEnumerable<TItem>> runner_result;
                List<TItem> result = new List<TItem>();
                Task<RunnerResult<IEnumerable<TItem>>> result_task;

                ProcessEnumParmeters(ref StartPosition, ref Advance, _defaultAdvance, nameof(GetRequiredAsync), trace_identifier, Logger);
                Task<Boolean> startup_task=StartRunningAsync();
                if(startup_task.IsCompleted) {
                    //Background process initialization has been already done or completed synchronously
                    #if TRACE
                    Logger?.LogTraceEnumerableRunnerBaseGetRequiredStartupComplete(Id, trace_identifier);
                    #endif
                    if(startup_task.IsCanceled) 
                        return new ValueTask<RunnerResult<IEnumerable<TItem>>>(
                            Task.FromCanceled<RunnerResult<IEnumerable<TItem>>>(new CancellationToken(true)));
                    if(startup_task.IsFaulted)
                        return new ValueTask<RunnerResult<IEnumerable<TItem>>>(
                            Task.FromException<RunnerResult<IEnumerable<TItem>>>(startup_task.Exception!.InnerExceptions[0]));
                    #if TRACE
                    Logger?.LogTraceEnumerableRunnerBaseGetRequiredTrySyncPath(Id, trace_identifier);
                    #endif
                    if(FetchAvailable(Advance, result, trace_identifier)) {
                        //Short path successfull: set correct Status
                        runner_result = FinishWithResult(result, trace_identifier);
                        #if TRACE
                        Logger?.LogTraceEnumerableRunnerBaseGetRequiredSyncExit(Id, trace_identifier);
                        #endif
                        return new ValueTask<RunnerResult<IEnumerable<TItem>>>(runner_result);
                    }
                    else {
                        //Come here if the short path failed: available data at current status cannot satisfy the request, so some async work is needed
                        #if TRACE
                        Logger?.LogTraceEnumerableRunnerBaseGetRequiredFormFetchTask(Id, trace_identifier);
                        #endif
                        _waitingTaskSource = new TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>();
                        result_task = _waitingTaskSource.Task;
                        AttacFetchResultProcessing(FetchRequiredAsync(Advance, result, Token, trace_identifier), 
                            new Context(result, Advance, trace_identifier, Token));
                    }
                }
                else {
                    //Background process initialisation is required and have not been completed synchronously
                    #if TRACE
                    Logger?.LogTraceEnumerableRunnerBaseGetRequiredFormStartupAndfetchTask(Id, trace_identifier);
                    #endif
                    _waitingTaskSource = new TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>();
                    result_task = _waitingTaskSource.Task;
                    startup_task.ContinueWith(SetCancelResult, trace_identifier, TaskContinuationOptions.OnlyOnCanceled);
                    startup_task.ContinueWith(SetFailResult, trace_identifier, TaskContinuationOptions.OnlyOnFaulted);
                    startup_task.ContinueWith(ContinueAsyncStartBackgroundProcessing,
                        new Context(result, Advance, trace_identifier, Token),
                        TaskContinuationOptions.OnlyOnRanToCompletion);
                }
                #if TRACE
                Logger?.LogTraceEnumerableRunnerBaseGetRequiredExitAsync(Id, trace_identifier);
                #endif
                return new ValueTask<RunnerResult<IEnumerable<TItem>>>(result_task);
            }
            catch(Exception exception) {
                Logger?.LogErrorEnumerableRunnerBaseGetRequiredException(exception, Id, trace_identifier);
                ReleasePseudoLock();
                #if TRACE
                Logger?.LogTraceEnumerableRunnerBasePseudoLockReleased(Id, trace_identifier);
                #endif
                throw;
            }
        }
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

        ///<summary>
        ///<inheritdoc/> 
        ///</summary>
        ///<remarks>
        ///Retunrs total number of elements already fetched by background process in the Progress result field.
        ///The EstimatedEnd result field will be left null until the runner background execution finishes.
        ///Otherwise it will contain the same value as Progress field.
        ///</remarks>
        ///<inheritdoc/>
        public override RunnerBkgProgress GetProgress()
        {
            CheckDisposed();
            Int32 progress = _queueAddedCount;
            return (progress, (IsBackgroundExecutionCompleted ? progress : null));
        }

        /// <inheritdoc/>
        public override Boolean IsBackgroundExecutionCompleted { get { CheckDisposed(); return _queue.IsAddingCompleted; } }

        ///<summary>
        ///Protected, overrides <see cref="RunnerBase.PreDispose">RunnerBase.PreDispose()</see>. 
        ///<inheritdoc path="/summary/toinherit/node()"/>
        ///</summary>
        ///<remarks>
        ///This method override terminates (via throwing an <see cref="ObjectDisposedException"/>) a task presenting 
        ///a result of an  async <see cref="GetRequiredAsync(int, CancellationToken, int, string?)">GetRequiredAsync</see> 
        ///call if such task exists.
        ///Effectively it forces termination of a pending call of the 
        ///<see cref="GetRequiredAsync(int, CancellationToken, int, string?)">GetRequiredAsync</see> method 
        ///with the aforementioned exception.
        ///</remarks>
        ///<inheritdoc/>
        protected override void PreDispose()
        {
            base.PreDispose();
            TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>? waiting_task_source = _waitingTaskSource;
            if(waiting_task_source!=null) {
                #if TRACE
                Logger?.LogTraceEnumerableRunnerBasePreDispose(Id);
                #endif
                waiting_task_source!.TrySetException(new ObjectDisposedException(DisposedObjectName()));
                ReleasePseudoLock();
                #if TRACE
                Logger?.LogTraceEnumerableRunnerBasePseudoLockReleased(Id, UNKNOWN_TRACE_IDENTIFIER);
                #endif
            }
        }

        ///<summary>
        ///Protected, overrides <see cref="RunnerBase.Dispose(bool)">RunnerBase.Dispose(bool)</see>, sealed. 
        ///<inheritdoc path="/summary/toinherit"/>
        ///</summary>
        ///<remarks>
        ///<inheritdoc path="/remarks/toinherit"/>
        /// In this class and in all descenet classes synchronous disposing is implemented via 
        /// starting asynchronous disposing and waiting its completion.
        ///<inheritdoc path="/remarks/nofinalizer"/>
        ///</remarks>
        ///<inheritdoc/>
        protected sealed override void Dispose(bool Disposing)
        {
            DisposeAsyncCore().Wait();
        }

        /// <summary>
        /// Protected virtual. <toinherit>This method performs a real work of disposing the object instance asynchronously.</toinherit>
        /// </summary>
        /// <returns>A task that presents asynchronous process of disposing the runner.</returns>
        /// <remarks>Disposes the base class stuff synchronously calling its <see cref="RunnerBase.Dispose(bool)"/> method. </remarks>
        protected virtual Task DisposeAsyncCore()
        {
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseDisposeCore(Id);
            #endif
            _queue.Dispose();
            base.Dispose(true);
            return Task.CompletedTask;
        }

        ///<summary>
        ///Protected, overrides <see cref="RunnerBase.DoAbort(string)">RunnerBase.DoAbort(string)</see>. 
        ///<inheritdoc path="/summary/toinherit/node()"/>
        ///</summary>
        ///<remarks>
        ///<inheritdoc path="/remarks/toinherit/node()"/>
        ///This method override tries to stop an execution a result fetching task via simulating completion of a background task.
        ///</remarks>
        ///<inheritdoc/>
        protected override void DoAbort(String TraceIdentifier)
        {
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAbortCore(Id, TraceIdentifier);
            #endif
            if(!Disposed()) try {
                    _queue.CompleteAdding();
                }
            catch(ObjectDisposedException) { };
            base.DoAbort(TraceIdentifier);
        }

        /// <summary>
        /// Protected abstract. 
        /// <toinherit>Creates a task representing an asynchronous fetch of results of the background processing to be returned by 
        /// <see cref="EnumerableRunnerBase{TItem}.GetRequiredAsync(int, CancellationToken, int, string?)">GetRequiredAsync</see> method.
        /// </toinherit>
        /// </summary>
        /// <param name="MaxAdvance">The maximum number of records to be fetched into the <paramref name="Result"/> list.</param>
        /// <param name="Result">The list holding the fetched records. May be partially filled before a call of this method.</param>
        /// <param name="Token">A CancellationToken instance that may be used to cancel the returned task.</param>
        /// <param name="TraceIdentifier">
        /// <inheritdoc cref="GetRequiredAsync(int, CancellationToken, int, string?)" path = '/param[@name="TraceIdentifier"]' />
        /// </param>
        /// <returns>A task that represents the process of fetching.</returns>
        protected internal abstract Task FetchRequiredAsync(Int32 MaxAdvance, List<TItem> Result, CancellationToken Token, String TraceIdentifier);

        /// <summary>
        /// Protected. Returns a boolean value indicating whether a background execution (that adds items to the queue) is completed.
        /// </summary>
        protected internal Boolean QueueIsAddingCompleted { get => _queue.IsAddingCompleted; }

        /// <summary>
        /// Protected. Marks the end of the background execution. That means that no more items will be added to the queue.
        /// </summary>
        protected internal void QueueCompleteAdding() => _queue.CompleteAdding();

        /// <summary>
        ///  Protected. Adds an item to the queue.
        /// </summary>
        /// <param name="Item">The item to be added</param>
        /// <returns>
        /// <see langword="true"/> if the item was successfully added , <see langword="false"/> overwise.
        /// </returns>
        /// <remarks>
        /// This method is really implemented via a <see cref="BlockingCollection{TItem}.TryAdd(TItem, int, CancellationToken)">
        /// BlockingCollection&lt;TItem&gt;.TryAdd(TItem, -1, CompletionToken) </see> accompanied by an interception 
        /// of <see cref="OperationCanceledException"/>. It returns <see langword="false"/> value then and only then 
        /// <see cref="IRunner.CompletionToken">CompletionToken</see> is canceled 
        /// (usually via <see cref="IRunner.Abort">Abort()</see> call).
        /// </remarks>
        protected internal Boolean QueueTryAdd(TItem Item)
        {
            Boolean result;
            try{
                result = _queue.TryAdd(Item, -1, CompletionToken);
            }
            catch (OperationCanceledException){
                #if TRACE
                Logger?.LogTraceEnumerableRunnerQueueAdditionCanceled(Id);
                #endif
                result = false;
            }
            if(result) _queueAddedCount++;
            return result;
        }

        /// <summary>
        /// Protected. Tries to remove the first item from the queue and return it if such an item exists.
        /// </summary>
        /// <param name="Item">The variable to which the removed item will be assigned if such an item exists</param>
        /// <returns><see langword="true"/> if the item was removed and assigned, <see langword="false"/> overwise.</returns>
        /// <remarks>
        /// The signature of this method is the same as one of <see cref="BlockingCollection{TItem}.TryTake(out TItem)"/>
        /// </remarks>
        protected internal Boolean QueueTryTake(out TItem Item) => _queue.TryTake(out Item!);

        /// <summary>
        /// Protected. Returns a number of itmes left in the queue.
        /// </summary>
        protected internal Int32 QueueCount => _queue.Count;

        void ThrowInvalidParallelism()
        {
            throw new InvalidOperationException(PARALLELISM_NOT_ALLOWED);
        }

        void ContinueAsyncStartBackgroundProcessing(Task FetchTask, Object? Context)
        {
            Context context = (Context as Context) ?? throw new ArgumentException(nameof(Context));
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAsyncStartBkgSuccess(Id,context.TraceIdentifier);
            #endif
            if(FetchAvailable(context.Advance, context.Accumulator, context.TraceIdentifier)) {
                //Short path successfull: set correct Status
                #if TRACE
                Logger?.LogTraceEnumerableRunnerBaseAsyncEnoughDataOnStartBkg(Id, context.TraceIdentifier);
                #endif
                FinishAndMakeResultBody(FetchTask, Context);
            }
            else {
                #if TRACE
                Logger?.LogTraceEnumerableRunnerBaseAsyncInsuffDataOnStartBkg(Id, context.TraceIdentifier);
                #endif
                AttacFetchResultProcessing(FetchRequiredAsync(context.Advance, context.Accumulator, context.Token, context.TraceIdentifier), context);
            }

        }

        void AttacFetchResultProcessing(Task FetchTask, Context Context)
        {
            FetchTask.ContinueWith(FinishAndMakeResultBody, Context, TaskContinuationOptions.OnlyOnRanToCompletion);
            FetchTask.ContinueWith(CancelResultBody, Context, TaskContinuationOptions.OnlyOnCanceled);
            FetchTask.ContinueWith(FailResultBody, Context, TaskContinuationOptions.OnlyOnFaulted);
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAsyncFetchContinuations(Id, Context.TraceIdentifier);
            #endif
        }

        void FailResultBody(Task FetchTask, Object? State)
        {
            Context context = (Context)(State ?? throw new ArgumentNullException(nameof(State)));
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAsyncFetchFailed(Id, context.TraceIdentifier);
            #endif
            StashOrphannedData(context.Accumulator, context.TraceIdentifier);
            SetFailResult(FetchTask, context.TraceIdentifier);
        }

        void SetFailResult(Task Antecedent, Object? TraceIdentifier)
        {
            String trace_identifier = (String?)TraceIdentifier ?? UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAsyncSetFailResult(Antecedent.Exception?.InnerExceptions[0], Id, trace_identifier);
            #endif
            TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>? waitingTaskSource = _waitingTaskSource;
            _waitingTaskSource = null;
            ReleasePseudoLock();
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBasePseudoLockReleased(Id, trace_identifier);
            #endif
            waitingTaskSource?.TrySetException(Antecedent.Exception!.InnerExceptions);
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAsyncFailResultSet(Id, trace_identifier);
            #endif
        }

        void CancelResultBody(Task _, Object? State)
        {
            Context context = (Context)(State ?? throw new ArgumentNullException(nameof(State)));
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAsyncFetchCanceled(Id, context.TraceIdentifier);
            #endif
            StashOrphannedData(context.Accumulator, context.TraceIdentifier);
            SetCancelResult(_, context.TraceIdentifier);
        }

        void SetCancelResult(Task _, Object? TraceIdentifier)
        {
            String trace_identifier = (String?)TraceIdentifier ?? UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAsyncSetCancelResult(Id, trace_identifier);
            #endif
            TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>? waitingTaskSource = _waitingTaskSource;
            _waitingTaskSource = null;
            ReleasePseudoLock();
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBasePseudoLockReleased(Id, trace_identifier);
            #endif
            waitingTaskSource?.TrySetCanceled();
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAsyncCancelResultSet(Id, trace_identifier);
            #endif
        }

        void FinishAndMakeResultBody(Task AntecedentTask, Object? State)
        {
            //We come here only if FetchTask is completed successfully
            Context context = (Context)(State ?? throw new ArgumentNullException(nameof(State)));
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAsyncFetchCompletedSuccess(Id, context.TraceIdentifier);
            #endif
            RunnerResult<IEnumerable<TItem>> result = MakeResultAndAdjustState(context.Accumulator, context.TraceIdentifier);
            TaskCompletionSource<RunnerResult<IEnumerable<TItem>>>? waitingTaskSource = _waitingTaskSource;
            _waitingTaskSource = null;
            ReleasePseudoLock();
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBasePseudoLockReleased(Id, context.TraceIdentifier);
            #endif
            waitingTaskSource?.TrySetResult(result);
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAsyncSuccessResultSet(Id, context.TraceIdentifier);
            #endif
        }

        void StashOrphannedData(List<TItem> Data, String TraceIdentifier)
        {
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAsyncStashOrphanedFetched(Id, TraceIdentifier);
            #endif
            Debug.Assert(_stashedFetch==null);
            _stashedFetch = Data;
        }

        RunnerResult<IEnumerable<TItem>> FinishWithResult(List<TItem> ResultList, String TraceIdentifier)
        {
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseMakeSyncResult(Id, TraceIdentifier);
            #endif
            RunnerResult<IEnumerable<TItem>> result = MakeResultAndAdjustState(ResultList, TraceIdentifier);
            ReleasePseudoLock();
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBasePseudoLockReleased(Id, TraceIdentifier);
            #endif
            return result;
        }

        RunnerResult<IEnumerable<TItem>> MakeResultAndAdjustState(List<TItem> ResultList, String TraceIdentifier)
        {
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseAsyncMakeResult(Id, TraceIdentifier);
            #endif
            Position = Position+ResultList.Count;
            if (_queue.Count==0 && _queue.IsAddingCompleted) {
                RunnerStatus new_status = Exception==null ? Completed : Failed;
                SetStatus(new_status);
                #if TRACE
                Logger?.LogTraceEnumerableRunnerBaseAsyncSetFinalStatus(Id, TraceIdentifier);
                #endif
            }
            RunnerResult<IEnumerable<TItem>> result = new RunnerResult<IEnumerable<TItem>>(ResultList, Status, Position, Status==Failed ? Exception : null);
            Logger?.LogDebugEnumerableRunnerResult(Status == Failed ? Exception : null, ResultList.Count, Status, Position, Id, TraceIdentifier);
            return result;
        }

        void ReleasePseudoLock()
        {
            Volatile.Write(ref _busy, 0);
        }

        Boolean TryAcquirePseudoLock()
        {
            return Interlocked.CompareExchange(ref _busy, 1, 0)==0;
        }

        void ProcessEnumParmeters(
            ref Int32 StartPosition,
            ref Int32 Advance,
            Int32 DefaultAdvance,
            String MethodName,
            String TraceIdentifier,
            ILogger? Logger = null)
        {
            String classname = Utilities.MakeClassCategoryName(GetType());
            if (StartPosition==CURRENT_POSITION)
                StartPosition=Position;
            if (StartPosition!=Position) {
                Logger?.LogWarningBadParam(MethodName, nameof(StartPosition), StartPosition, Id, TraceIdentifier);
                throw new ArgumentException(nameof(StartPosition),$"{classname}.{MethodName}: A start position requested ({StartPosition}) differs from the current one({Position})");
            }
            if (Advance==DEFAULT_ADVANCE) Advance=DefaultAdvance;
            if (Advance<=0) {
                Logger?.LogWarningBadParam(MethodName, nameof(Advance), Advance, Id, TraceIdentifier);
                throw new ArgumentException(nameof(Advance), $"{classname}.{MethodName}: Invalid advance value: {Advance}");
            }

        }

        internal Boolean FetchAvailable(Int32 MaxAdvance, List<TItem> Result, String? TraceIdentifier=null)
        //Returns true if all available results fetched, internal access is added for testing purposes
        {
            TItem? item;
            Boolean result;
            String trace_identifier = TraceIdentifier ?? UNKNOWN_TRACE_IDENTIFIER;

            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseFetchAvailable(Id, trace_identifier);
            #endif
            if(Status.IsFinal()) {
                #if TRACE
                Logger?.LogTraceEnumerableRunnerBaseFetchAvailableFinal(Id, trace_identifier);
                result = true;
                #endif

            }
            else {
                int fetched_count = Result.Count;
                if(_stashedFetch != null) {
                    //Fetch from orphanned results of previosly cancelled or failed  GetRequiredAsync
                    int orphanned_count = _stashedFetch.Count;
                    if(orphanned_count <= MaxAdvance - fetched_count) {
                        #if TRACE
                        Logger?.LogTraceEnumerableRunnerBaseFetchAvailableStashedAll(Id, trace_identifier);
                        #endif
                        Result.AddRange(_stashedFetch);
                        fetched_count += orphanned_count;
                        _stashedFetch = null;
                    }
                    else {
                        #if TRACE
                        Logger?.LogTraceEnumerableRunnerBaseFetchAvailableStashedPartial(Id, trace_identifier);
                        #endif
                        Result.AddRange(_stashedFetch!.GetRange(0, MaxAdvance - fetched_count));
                        _stashedFetch.RemoveRange(0, MaxAdvance - fetched_count);
                        fetched_count = MaxAdvance;
                    }
                }
                //Fetch from current queue
                #if TRACE
                Logger?.LogTraceEnumerableRunnerBaseFetchAvailableFromQueue(Id, trace_identifier);
                #endif
                for(; fetched_count < MaxAdvance && _queue.TryTake(out item); fetched_count++) Result.Add(item);
                result=fetched_count >= MaxAdvance || _queue.IsAddingCompleted && _queue.Count == 0;
            }
            #if TRACE
            Logger?.LogTraceEnumerableRunnerBaseFetchAvailableExit(Id, trace_identifier, result);
#           endif
            return result;
        }

        class Context
        {
            public List<TItem> Accumulator { get; init; }
            public String TraceIdentifier { get; init; }
            public Int32 Advance { get; init; }
            public CancellationToken Token { get; init; }
            public Context(List<TItem> Accumulator, Int32 Advance, String TraceIdentifier, CancellationToken Token)
            {
                this.Accumulator = Accumulator;
                this.TraceIdentifier = TraceIdentifier;
                this.Advance = Advance;
                this.Token = Token;
            }
        }

    }
}
