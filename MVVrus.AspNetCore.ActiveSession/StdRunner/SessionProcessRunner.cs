using MVVrus.AspNetCore.ActiveSession.Internal;
using System; 

namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// A class used to create runners that execute background task. 
    /// The executing task invokes from time to time a callback presented by the runner, 
    /// informing it about background execution at the time of the invocation.                                    
    /// </summary>
    /// <typeparam name="TResult"> 
    /// <inheritdoc cref="IRunner{TResult}" path='/typeparam[@name="TResult"]'/>
    /// Values of this type are passed to a callback and a value of this type is optionally returned by a background task.
    /// </typeparam>
    /// <remarks>
    /// <list type="number">
    /// <item>
    /// The callback is a delegate of type <see cref="Action{T1, T2}">Action&lt;TResult, Int32?&gt;</see>, accepting two values:
    /// an intermediate result of the background process 
    /// and an estimation of final <see cref="IRunner.Position"/> value after completion of the background task.
    /// If this estimation cannot be given the backgrond task should pass <see langword="null"/> instead of it.
    /// </item>
    /// <item>
    /// The value of the <see cref="IRunner.Position"/> property of runners of this class 
    /// indicates a number of invocations of the callback was made so far plus one more if the background task has run to completion.
    /// </item>
    /// <item>
    /// Delegates passed to constructors of this class must accept two parameters: 
    /// the callback delegate to be periodically invoked (the runner code passes its internal method via this parameter)
    /// and a <see cref="CancellationToken"/> that can be used for cooperative cancellation of the background task
    /// (the runner code passes its <see cref="IRunner.CompletionToken"/> via this parameter).
    /// </item>
    /// <item>
    /// Delegates passed to constructors of this class belongs to one of two categories: one that directly creates the background task 
    /// (i.e. retruns result of type <see cref="Task"/> or its descendant, see below)
    /// and the other that serves as a body of a background task executed synchronously by a thread from the thread pool.
    /// The later delegates may return <typeparamref name="TResult"/> or return nothing.
    /// </item>
    /// <item>
    /// The runners belonging to this class can deal with two different types of background tasks: 
    /// one that returns the final result (i.e. having type <see cref="Task{TResult}"/>)
    /// and the other that does not return any result (i.e. having type <see cref="Task"/>), 
    /// the result passed by the last callback invoked being assumed as a final one.
    /// A type of a delegate passed to the instance constructor determines a type of the background task for the instance created:
    /// if the delegate returns <see cref="Task{TResult}"/> or just <typeparamref name="TResult"/> 
    /// then the background task returns a final result,
    /// if the delegate returns <see cref="Task"/> or does not return anything 
    /// then the background task does not return a final result.
    /// </item>
    /// <item>
    /// Also there are two sorts of constructors for this class: 
    /// one that creates instance that uses an internal <see cref="CancellationTokenSource"/> for its <see cref="IRunner.CompletionToken"/>
    /// and the other that can accept an external <see cref="CancellationTokenSource"/> for this purpose 
    /// (see description of <see cref="RunnerBase.RunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?)">RunnerBase
    /// constructor</see> parameters ). 
    /// The former accepts a delegate as its first parameter while the later accepts as its first parameter a tuple of 3 values:
    /// a delegate, a  <see cref="CancellationTokenSource"/> and a <see cref="Boolean"/> 
    /// value indicating passing ownership of the second value. 
    /// </item>
    /// </list>
    /// </remarks>
    public class SessionProcessRunner<TResult> : RunnerBase, IRunner<TResult>, IAsyncDisposable
    {
        TResult _result=default!;
        Int32 _progress = 0;
        Int32? _estimatedEnd=null;
        Object _lock = new Object();
        PriorityQueue<TaskListItem, Int32> _waitList = new PriorityQueue<TaskListItem, Int32>();
        readonly Func<Action<TResult, Int32?>, CancellationToken, Task> _taskToRunCreator;
        RunnerStatus _backgroundStatus = RunnerStatus.Stalled;
        Exception? _backgroundException = null;
        internal Task? _bkgCompletionTask;  //internal asccess modifier is for test project access
        Boolean _isBackgroundExecutionCompleted=false;
        Boolean _bkgTaskReturnsResult = false;
        Boolean _bkgSynchronous = false;
        Task? _disposeTask;

        /// <summary>
        /// <factory>A constructor that creates an instance of the runner by means of <see cref="TypeRunnerFactory{TRequest, TResult}">TypeRunnerFactory</see>.</factory>
        /// <noextcts>An external source for CompletionToken property of the runner cannot be used. </noextcts>
        /// <sync>The runner will run a background task that executes the specified (synchronous) delegate </sync> 
        /// <result>returning the final result from the task.</result> 
        /// </summary>
        /// <param name="ProcessTaskBody">A delegate that is used as a synchronously executing body of a background task.</param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Logger">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="Logger"]'/>
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor] 
        public SessionProcessRunner(Func<Action<TResult, Int32?>, CancellationToken, TResult> ProcessTaskBody, 
            RunnerId RunnerId, ILogger<SessionProcessRunner<TResult>>? Logger) :
            this((ProcessTaskBody??throw new ArgumentNullException(nameof(ProcessTaskBody)),
                null, true), RunnerId, Logger) {}

        /// <summary>
        /// <inheritdoc path="/summary/factory" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <extcts>An external source for CompletionToken property of the runner can be used. </extcts>
        /// <inheritdoc path="/summary/sync" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/result" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// </summary>
        /// <param name="Param"> A tuple of three values:
        /// <list type="bullet">
        /// <item>
        /// <see cref="Func{T1, T2, TResult}">Func&lt;Action&lt;TResilt, int?&gt;, CancellationToken, TResult&gt; </see> ProcessTaskBody: <br/>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"  path='/param[@name="ProcessTaskBody"]' />
        /// </item>
        /// <item copy="yes">
        /// <see cref="CancellationTokenSource"/> Cts: <br/> 
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="CompletionTokenSource"]'/>
        /// </item>
        /// <item copy="yes"><see cref="Boolean"/> PassCtsOwnership: <br/>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="PassCtsOwnership"]'/>
        /// </item>
        /// </list>
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Logger">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="Logger"]'/>
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            (Func<Action<TResult, Int32?>, CancellationToken, TResult> ProcessTaskBody, CancellationTokenSource? Cts, Boolean PassCtsOwnership) Param, 
            RunnerId RunnerId, ILogger<SessionProcessRunner<TResult>>? Logger) :
            this(MakeTaskToRunCreator(Param.ProcessTaskBody??throw new ArgumentNullException(nameof(Param.ProcessTaskBody))),
                Param.Cts, Param.PassCtsOwnership, RunnerId,
                Logger) 
        {
            _bkgTaskReturnsResult=true;
            _bkgSynchronous=true;
        }

        /// <summary>
        /// <inheritdoc path="/summary/factory" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/noextcts" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/sync" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <noresult>without returning the final result from the task.</noresult> 
        /// </summary>
        /// <param name="ProcessTaskBody">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"  path='/param[@name="ProcessTaskBody"]' />
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Logger">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="Logger"]'/>
        /// </param>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            Action<Action<TResult, Int32?>, CancellationToken> ProcessTaskBody, RunnerId RunnerId, ILogger<SessionProcessRunner<TResult>>? Logger):
            this((ProcessTaskBody??throw new ArgumentNullException(nameof(ProcessTaskBody)), null, true),
                RunnerId, Logger) {}

        /// <summary>
        /// <inheritdoc path="/summary/factory" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/extcts" cref="SessionProcessRunner{TResult}.SessionProcessRunner(ValueTuple{Func{Action{TResult, int?}, CancellationToken, TResult}, CancellationTokenSource?, bool}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/sync" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/noresult" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Action{Action{TResult, int?}, CancellationToken}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// </summary>
        /// <param name="Param">A tuple of three values: 
        /// <list type="bullet">
        /// <item>
        /// <see cref="Action{T1, T2}">Action&lt;Action&lt;TResilt, Int32?&gt;, CancellationToken&gt; </see> ProcessTaskBody: <br/>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"  path='/param[@name="ProcessTaskBody"]' />
        /// </item>
        /// <inheritdoc path='/param[@name="Param"]/list/item[@copy]' cref="SessionProcessRunner{TResult}.SessionProcessRunner(ValueTuple{Func{Action{TResult, int?}, CancellationToken, TResult}, CancellationTokenSource?, bool}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// </list>
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Logger">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="Logger"]'/>
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            (Action<Action<TResult, Int32?>, CancellationToken> ProcessTaskBody, 
            CancellationTokenSource? Cts, Boolean PassCtsOwnership) Param, RunnerId RunnerId, ILogger<SessionProcessRunner<TResult>>? Logger) :
            this(MakeTaskToRunCreator(Param.ProcessTaskBody??throw new ArgumentNullException(nameof(Param.ProcessTaskBody))),
                Param.Cts, Param.PassCtsOwnership, RunnerId,
                Logger)
        {
            _bkgSynchronous=true;
        }

        /// <summary>
        /// <inheritdoc path="/summary/factory" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/noextcts" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <async>The runner will run a background task created by the specified delegate </async>
        /// <inheritdoc path="/summary/result" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// </summary>
        /// <param name="ProcessTaskCreator">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="ProcessTaskCreator"]'/>
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Logger">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="Logger"]'/>
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(Func<Action<TResult, Int32?>, CancellationToken, Task<TResult>> ProcessTaskCreator, 
            RunnerId RunnerId, ILogger<SessionProcessRunner<TResult>>? Logger) :
            this((ProcessTaskCreator??throw new ArgumentNullException(nameof(ProcessTaskCreator)), null, true), RunnerId, Logger)
        {}

        /// <summary>
        /// <inheritdoc path="/summary/factory" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/noextcts" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/async" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task{TResult}}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/noresult" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Action{Action{TResult, int?}, CancellationToken}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// </summary>
        /// <param name="ProcessTaskCreator">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="ProcessTaskCreator"]'/>
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Logger">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="Logger"]'/>
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(Func<Action<TResult, Int32?>, CancellationToken, Task> ProcessTaskCreator, 
            RunnerId RunnerId, ILogger<SessionProcessRunner<TResult>>? Logger) :
            this((ProcessTaskCreator??throw new ArgumentNullException(nameof(ProcessTaskCreator)), null, true), RunnerId, Logger)
        { }

        /// <summary>
        /// <inheritdoc path="/summary/factory" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/extcts" cref="SessionProcessRunner{TResult}.SessionProcessRunner(ValueTuple{Func{Action{TResult, int?}, CancellationToken, TResult}, CancellationTokenSource?, bool}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/async" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task{TResult}}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/result" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// </summary>
        /// <param name="Param"> A tuple of three values:
        /// <list type="bullet">
        /// <item>
        /// <see cref="Func{T1, T2, TResult}">Func&lt;Action&lt;TResilt, Int32?&gt;, CancellationToken, Task&lt;TResult&gt;&gt; </see> ProcessTaskCreator: <br/>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="ProcessTaskCreator"]'/>
        /// </item>
        /// <inheritdoc path='/param[@name="Param"]/list/item[@copy]' cref="SessionProcessRunner{TResult}.SessionProcessRunner(ValueTuple{Func{Action{TResult, int?}, CancellationToken, TResult}, CancellationTokenSource?, bool}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// </list>
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Logger">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="Logger"]'/>
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            (Func<Action<TResult, Int32?>, CancellationToken, Task<TResult>> ProcessTaskCreator, CancellationTokenSource? Cts, Boolean PassCtsOwnership) Param, 
            RunnerId RunnerId, ILogger<SessionProcessRunner<TResult>>? Logger) :
            this(Param.ProcessTaskCreator??throw new ArgumentNullException(nameof(Param.ProcessTaskCreator)), null, true, RunnerId,
                Logger)
        {
            _bkgTaskReturnsResult=true;
        }

        /// <summary>
        /// <inheritdoc path="/summary/factory" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, TResult}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/extcts" cref="SessionProcessRunner{TResult}.SessionProcessRunner(ValueTuple{Func{Action{TResult, int?}, CancellationToken, TResult}, CancellationTokenSource?, bool}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/async" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task{TResult}}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// <inheritdoc path="/summary/noresult" cref="SessionProcessRunner{TResult}.SessionProcessRunner(Action{Action{TResult, int?}, CancellationToken}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// </summary>
        /// <param name="Param"> A tuple of three values:
        /// <list type="bullet">
        /// <item>
        /// <see cref="Func{T1, T2, TResult}">Func&lt;Action&lt;TResilt, Int32?&gt;, CancellationToken, Task&gt; </see> ProcessTaskCreator: <br/>
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="ProcessTaskCreator"]'/>
        /// </item>
        /// <inheritdoc path='/param[@name="Param"]/list/item[@copy]' cref="SessionProcessRunner{TResult}.SessionProcessRunner(ValueTuple{Func{Action{TResult, int?}, CancellationToken, TResult}, CancellationTokenSource?, bool}, RunnerId, ILogger{SessionProcessRunner{TResult}}?)"/>
        /// </list>
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Logger">
        /// <inheritdoc cref="SessionProcessRunner{TResult}.SessionProcessRunner(Func{Action{TResult, int?}, CancellationToken, Task}, CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="Logger"]'/>
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        [ActiveSessionConstructor]
        public SessionProcessRunner(
            (Func<Action<TResult, Int32?>, CancellationToken, Task> ProcessTaskCreator, CancellationTokenSource? Cts, Boolean PassCtsOwnership) Param,
            RunnerId RunnerId, ILogger<SessionProcessRunner<TResult>>? Logger) :
            this(Param.ProcessTaskCreator??throw new ArgumentNullException(nameof(Param.ProcessTaskCreator)), null, true, RunnerId,
                Logger)
        { }

        /// <summary>
        /// A constructor that performs real work of creating an instance of the runner of this class.
        /// This is a protected constructor that is used by all public constructors of this class 
        /// and may be used by constructors of descendent classes.
        /// </summary>
        /// <param name="ProcessTaskCreator">
        /// A delegate that is used to create a background task. 
        /// </param>
        /// <param name="CompletionTokenSource">
        /// <inheritdoc cref="RunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="CompletionTokenSource"]'/>
        /// </param>
        /// <param name="PassCtsOwnership">
        /// <inheritdoc cref="RunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="PassCtsOwnership"]'/>
        /// </param>
        /// <param name="RunnerId">
        /// <inheritdoc cref="RunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="RunnerId"]'/>
        /// </param>
        /// <param name="Logger">
        /// <inheritdoc cref="RunnerBase(CancellationTokenSource?, bool, RunnerId, ILogger?)"  path='/param[@name="Logger"]'/>
        /// </param>
        /// <remarks>
        /// In some cases the delagate <paramref name="ProcessTaskCreator"/> really creates a task of type <see cref="Task{TResult}"/>.
        /// </remarks>
        protected SessionProcessRunner(
            Func<Action<TResult, Int32?>,CancellationToken,Task> ProcessTaskCreator, 
            CancellationTokenSource? CompletionTokenSource, Boolean PassCtsOwnership,
            RunnerId RunnerId, ILogger? Logger) 
            : base(CompletionTokenSource, PassCtsOwnership, RunnerId, Logger)
        {
            Logger?.LogDebugSessionRunnerConstructor(RunnerId, CompletionTokenSource!=null, PassCtsOwnership, _bkgSynchronous, _bkgTaskReturnsResult);
            _taskToRunCreator = ProcessTaskCreator;
            StartRunning();
        }

        /// <summary>
        /// Protected, overrides <see cref="RunnerBase.StartBackgroundExecution"/>
        /// <inheritdoc path="/summary/toinherit/node()"/>
        /// </summary>
        /// <remarks>
        /// This override creates a background task using a delegate, passed to a constructor. 
        /// Then it starts the task, if it's not started.
        /// </remarks>
        protected internal override void StartBackgroundExecution()
        {
            #if TRACE
            Logger?.LogTraceSessionProcessStartBackgroundExecution(Id);
            #endif
            Task t = _taskToRunCreator(SetProgress, CompletionToken);
            _bkgCompletionTask=t.ContinueWith(SessionTaskCompletionHandler,TaskContinuationOptions.ExecuteSynchronously);
            if(t.Status == TaskStatus.Created) { 
                try { t.Start(); } catch(TaskSchedulerException) { }
                #if TRACE
                Logger?.LogTraceSessionProcessStartBackgroundTask(Id);
                #endif
            }
            #if TRACE
            Logger?.LogTraceSessionProcessStartBackgroundExecutionExit(Id);
            #endif
        }

        static Func<Action<TResult, Int32?>, CancellationToken, Task> MakeTaskToRunCreator(
            Action<Action<TResult, Int32?>, CancellationToken> TaskBody)
        {
            return (ProgressSetter,Token)=>new Task(
                State => TaskBody(
                    ((ValueTuple<Action<TResult, Int32?>, CancellationToken>)State!).Item1,
                    ((ValueTuple<Action<TResult, Int32?>, CancellationToken>)State!).Item2),
                (ProgressSetter, Token),Token);
        }

        static Func<Action<TResult, Int32?>, CancellationToken, Task> MakeTaskToRunCreator(
            Func<Action<TResult, Int32?>, CancellationToken,TResult> TaskBody)
        {
            return (ProgressSetter, Token) => new Task<TResult>(
                State => TaskBody(
                    ((ValueTuple<Action<TResult, Int32?>, CancellationToken>)State!).Item1,
                    ((ValueTuple<Action<TResult, Int32?>, CancellationToken>)State!).Item2)
                , (ProgressSetter, Token), Token)
            ;
        }

        /// <summary>
        ///Protected, overrides <see cref="RunnerBase.PreDispose">RunnerBase.PreDispose()</see>. 
        ///<inheritdoc path="/summary/toinherit/node()"/>
        /// </summary>
        /// <remarks>
        /// This method override sends a signal to a task performing the background process to termenate itself. 
        /// The signal is sent via canceling <see cref="IRunner.CompletionToken"/> 
        /// during execution of the <see cref="IRunner.Abort(string?)"/> method.
        /// </remarks>
        protected override void PreDispose()
        {
            #if TRACE
            Logger?.LogTraceSessionProcessAbortBkgTask(Id);
            #endif
            Abort();
            base.PreDispose();
        }

        ///<inheritdoc cref="EnumerableRunnerBase{TItem}.DisposeAsyncCore"/>
        protected virtual async Task DisposeAsyncCore()
        {
            #if TRACE
            Logger?.LogTraceSessionProcessDisposing(Id);
            #endif
            if(_bkgCompletionTask!=null) {
                await _bkgCompletionTask!;
                #if TRACE
                Logger?.LogTraceSessionProcessBkgTaskAwaited(Id);
                #endif
            }
            base.Dispose(true);
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            if(SetDisposed()) {
                #if TRACE
                Logger?.LogTraceSessionProcessDisposeAsync(Id);
                #endif
                _disposeTask = DisposeAsyncCore();
            }
            return _disposeTask!.IsCompleted ? ValueTask.CompletedTask : new ValueTask(_disposeTask!);
        }

        ///<inheritdoc cref="EnumerableRunnerBase{TItem}.Dispose(bool)"/>
        protected sealed override void Dispose(Boolean Disposing)
        {
            DisposeAsyncCore().Wait();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <interprete>
        /// <para>
        /// If <paramref name="Advance"/> has zero (i.e. <see cref="IRunner.DEFAULT_ADVANCE"/>) value 
        /// and <paramref name="StartPosition"/> has (explicitly or implicitly, using <see cref="IRunner.CURRENT_POSITION"/>) the same value 
        /// as the current <see cref="IRunner.Position"/> property then value of the Advance is assumed to be 1.
        /// </para>
        /// <para>
        /// This method always returns a result (value of Result field in the <see cref="RunnerResult{TResult}">RunnerResult</see> structure) 
        /// obtained from the last callback from the background process 
        /// (or its returned result if the background process has been ended and returns a result).
        /// So when the method is called for the earlier resulting position than achieved by the background process 
        /// it does return not an intermediate result for that position 
        /// but the last result for the current position achieved by background.
        /// </para>
        /// </interprete>
        /// </remarks>
        public RunnerResult<TResult> GetAvailable(Int32 Advance = IRunner.MAXIMUM_ADVANCE, Int32 StartPosition = IRunner.CURRENT_POSITION, String? TraceIdentifier = null)
        {
            RunnerResult<TResult> result;
            String trace_identifier = TraceIdentifier??ActiveSessionConstants.UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            Logger?.LogTraceSessionProcessGetAvailableEntered(Id, trace_identifier);
            #endif
            lock(_lock) {
                CheckDisposed();
                #if TRACE
                Logger?.LogTraceSessionProcessGetAvailableLockAckuired(Id, trace_identifier);
                #endif
                CheckAndNormalizeParams(ref Advance, ref StartPosition, nameof(GetAvailable), trace_identifier);
                RunnerStatus new_status=Status;
                Int32 position = Position;
                if(!Status.IsFinal()) {
                    Int32 max_advance = _progress-StartPosition;
                    if(max_advance<=Advance) {
                        new_status=_backgroundStatus; //Stalled or a final 
                        position =_progress;
                        #if TRACE
                        Logger?.LogTraceSessionProcessGetAvailableAll(Id,trace_identifier);
                        #endif
                    }
                    else {
                        new_status=RunnerStatus.Progressed;
                        position =StartPosition+Advance;
                        #if TRACE
                        Logger?.LogTraceSessionProcessGetAvailableNotAll(Id,trace_identifier);
                        #endif
                    }
                }
                result = MakeResultAndAdjustState(_result, new_status, position, trace_identifier, false);
            }
            #if TRACE
            Logger?.LogTraceSessionProcessGetAvailableLockReleased(Id,trace_identifier);
            #endif
            Logger?.LogDebugSessionProcessRunnerResult(result.FailureException, result.Result?.ToString()??"<null>", 
                result.Status, result.Position, Id, trace_identifier);
            return result;
        }

        ///<inheritdoc/>
        ///<remarks><inheritdoc cref="GetAvailable(int, int, string?)" /></remarks>
        public ValueTask<RunnerResult<TResult>> GetRequiredAsync(
            Int32 Advance = IRunner.DEFAULT_ADVANCE,
            CancellationToken Token = default,
            Int32 StartPosition = IRunner.CURRENT_POSITION,
            String? TraceIdentifier = null)
        {
            ValueTask<RunnerResult<TResult>> result_task;
            String trace_identifier = TraceIdentifier??ActiveSessionConstants.UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            Logger?.LogTraceSessionProcessGetRequiredAsyncEntered(Id, trace_identifier);
            #endif
            lock(_lock) {
                CheckDisposed();
                #if TRACE
                Logger?.LogTraceSessionProcessGetRequiredAsyncLockAckuired(Id, trace_identifier);
                #endif
                int max_advance;
                CheckAndNormalizeParams(ref Advance, ref StartPosition, nameof(GetRequiredAsync), trace_identifier);
                max_advance = Math.Min(Advance, Int32.MaxValue-StartPosition);
                if(Status.IsFinal() || IsBackgroundExecutionCompleted || max_advance <= _progress-StartPosition) { //Synchronous
                    #if TRACE
                    Logger?.LogTraceSessionProcessGetRequiredAsyncSynchronous(Id, trace_identifier);
                    #endif
                    Int32 position  = Math.Min(_progress, StartPosition + max_advance);
                    RunnerStatus new_status;
                    if(position  < _progress) {
                        new_status = RunnerStatus.Progressed;
                        #if TRACE
                        Logger?.LogTraceSessionProcessGetRequiredAsyncNotAll(Id, trace_identifier);
                        #endif
                    }
                    else {
                        new_status=_backgroundStatus;
                        #if TRACE
                        Logger?.LogTraceSessionProcessGetRequiredAsyncAll(Id,trace_identifier);
                        #endif
                    }
                    RunnerResult<TResult> result = MakeResultAndAdjustState(_result, new_status, position, trace_identifier, false);
                    Logger?.LogDebugSessionProcessRunnerResult(result.FailureException, result.Result?.ToString()??"<null>",
                        result.Status, result.Position, Id, trace_identifier);
                    result_task=new ValueTask<RunnerResult<TResult>>(result);
                }
                else { //Asynchronous path
                    #if TRACE
                    Logger?.LogTraceSessionProcessGetRequiredAsyncAsynchronous(Id, trace_identifier);
                    #endif
                    TaskCompletionSource<RunnerResult<TResult>> tcs = new TaskCompletionSource<RunnerResult<TResult>>();
                    TaskListItem task_item = new TaskListItem
                    {
                        TaskSourceToComplete = tcs,
                        Token = Token,
                        DesiredPosition = StartPosition+max_advance,
                        TraceIdentifier = trace_identifier
                    };
                    if(Token.CanBeCanceled) task_item.Registration = Token.Register(CancelATask, task_item);
                    _waitList.Enqueue(task_item, task_item.DesiredPosition);
                    #if TRACE
                    Logger?.LogTraceSessionProcessGetRequiredAsyncTaskEnqueued(Id, trace_identifier);
                    #endif
                    result_task=new ValueTask<RunnerResult<TResult>>(tcs.Task);
                }
            }
            #if TRACE
            Logger?.LogTraceSessionProcessGetRequiredAsyncLockReleased(Id, trace_identifier);
            #endif
            return result_task;
        }

        /// <inheritdoc/>
        public override RunnerBkgProgress GetProgress()
        {
            lock(_lock) return (_progress, _estimatedEnd);
        }


        /// <inheritdoc/>
        public override Boolean IsBackgroundExecutionCompleted { get => _isBackgroundExecutionCompleted; }

        void CheckAndNormalizeParams(ref Int32 Advance,ref Int32 StartPosition, String MethodName, String TraceIdentifier)
        {
            Exception? ex = null;
            #if TRACE
            Logger?.LogTraceSessionProcessCheckAndNormalizeParams(Id,TraceIdentifier);
            #endif
            if(StartPosition < Position && StartPosition != IRunner.CURRENT_POSITION) {
                String classname = Utilities.MakeClassCategoryName(GetType());
                ex=new ArgumentException(nameof(StartPosition),
                        $"{classname}.{MethodName}:StartPosition value ({StartPosition}) is behind current runner Position({Position})");
                Logger?.LogWarningSessionProcessBadParameters(ex,Id,TraceIdentifier);
                throw ex;
            }
            if(Advance<0) {
                String classname = Utilities.MakeClassCategoryName(GetType());
                ex=new ArgumentException(nameof(Advance),
                        $"{classname}.{MethodName}:Advance value ({Advance}) is negative");
                Logger?.LogWarningSessionProcessBadParameters(ex,Id,TraceIdentifier);
                throw ex;
            }
            if(StartPosition== IRunner.CURRENT_POSITION  && Advance == IRunner.DEFAULT_ADVANCE) {
                Advance = 1;
                #if TRACE
                Logger?.LogTraceSessionProcessCheckAndNormalizeParamsDefaultAdjusted(Id,TraceIdentifier);
                #endif
            }
            if(StartPosition == IRunner.CURRENT_POSITION) StartPosition = Position ;
            #if TRACE
            Logger?.LogTraceSessionProcessCheckAndNormalizeParamsExit(Id,TraceIdentifier);
            #endif
        }

        void CancelATask(object? Item)
        {
            TaskListItem task_item = Item as TaskListItem ?? throw new ArgumentNullException(nameof(Item)); 
            #if TRACE
            Logger?.LogTraceSessionProcessPendingTaskSetCanceled(Id,task_item.TraceIdentifier);
            #endif
            if(task_item.TaskSourceToComplete?.TrySetCanceled()??false) 
                Logger?.LogDebugGetRequiredAsyncCanceled(Id, task_item.TraceIdentifier);
            else Logger?.LogWarningTaskOutcomeAlreadySet(Id,task_item.TraceIdentifier);
            task_item.TaskSourceToComplete = null; //To avoid an excessive attempt to complete the task
                                                   //defined by this task_item.TaskSourceToComplete in the AdvanceProgress method
        }

        void SessionTaskCompletionHandler(Task Antecedent)
        {
            TaskListItem? task_item;
            Int32 task_position;
            #if TRACE
            Logger?.LogTraceSessionProcessBkgEnded(Id);
            #endif
            lock(_lock) {
                #if TRACE
                Logger?.LogTraceSessionProcessBkgEndedLockAcquired(Id);
                #endif
                _isBackgroundExecutionCompleted = true;
                switch(Antecedent.Status) {
                    case TaskStatus.RanToCompletion:
                        #if TRACE
                        Logger?.LogTraceSessionProcessBkgEndedRanToCompletion(Id);
                        #endif
                        _backgroundStatus=RunnerStatus.Completed;
                        _progress++;
                        if(_bkgTaskReturnsResult) {
                            #if TRACE
                            Logger?.LogTraceSessionProcessBkgEndedAcceptResult(Id);
                            #endif
                            _result = ((Task<TResult>)Antecedent).Result;
                        }
                        break;
                    case TaskStatus.Faulted:
                        Exception? exception = Antecedent.Exception;
                        if(exception != null && exception is AggregateException aggregate_exception)
                            if(aggregate_exception.InnerExceptions.Count == 1) exception = aggregate_exception.InnerExceptions[0];
                        if(exception is OperationCanceledException) {
                            #if TRACE
                            Logger?.LogTraceSessionProcessBkgEndedCanceled(Id);
                            #endif
                            Abort();
                        }
                        else {
                            #if TRACE
                            Logger?.LogTraceSessionProcessBkgEndedFaulted(exception, Id);
                            #endif
                            _backgroundStatus=RunnerStatus.Failed;
                            _backgroundException=exception;
                        }
                        break;
                    case TaskStatus.Canceled:
                        #if TRACE
                        Logger?.LogTraceSessionProcessBkgEndedCanceled(Id);
                        #endif
                        Abort();
                        break;
                    default:
                        Logger?.LogErrorSessionProgressBkgEndedInternal(Antecedent.Status, Id);
                        String msg = $"Internal error in SessionTaskCompletionHandler: attempt to continue a task with Status={Antecedent.Status}";
                        while(_waitList.TryDequeue(out task_item, out task_position)) {
                            if(task_item.TaskSourceToComplete!=null) {
                                Exception e = new InvalidOperationException(msg);
                                #if TRACE
                                Logger?.LogTraceSessionProcessPendingTaskSetException(e,Id,task_item.TraceIdentifier);
                                #endif
                                if(task_item.TaskSourceToComplete.TrySetException(e)) 
                                    Logger?.LogDebugGetRequiredAsyncFailed(e,Id,task_item.TraceIdentifier);
                            }
                        }
                        return;
                }
                _estimatedEnd = _progress;
                #if TRACE
                Logger?.LogTraceSessionProcessBkgEndedCompletePendingTasks(Id);
                #endif
                try {
                    while(_waitList.TryDequeue(out task_item, out task_position)) {
                        if(task_item.TaskSourceToComplete!=null) {
                            #if TRACE
                            Logger?.LogTraceSessionProcessBkgEndedCompleteAPendingTask(Id, task_item.TraceIdentifier);
                            #endif
                            if(Disposed()) {
                                Exception e = new ObjectDisposedException(DisposedObjectName());
                                #if TRACE
                                Logger?.LogTraceSessionProcessPendingTaskSetException(e, Id, task_item.TraceIdentifier);
                                #endif
                                if(task_item.TaskSourceToComplete.TrySetException(e))
                                    Logger?.LogTraceSessionProcessPendingTaskSetException(e, Id, task_item.TraceIdentifier);
                                else Logger?.LogWarningTaskOutcomeAlreadySet(Id, task_item.TraceIdentifier);
                            }
                            else {
                                RunnerStatus status = _backgroundStatus;
                                if(Status.IsFinal()) status=Status;
                                RunnerResult<TResult> result = MakeResultAndAdjustState(_result, status, _progress, task_item.TraceIdentifier, true);
                                #if TRACE
                                Logger?.LogTraceSessionProcessPendingTaskSetResult(Id, task_item.TraceIdentifier);
                                #endif
                                if(task_item.TaskSourceToComplete.TrySetResult(result)) {
                                    Position=_progress;
                                    if(SetStatus(_backgroundStatus) && _backgroundStatus==RunnerStatus.Failed) Exception=_backgroundException;
                                    Logger?.LogDebugSessionProcessRunnerResult(result.FailureException, result.Result?.ToString()??"<null>",
                                        result.Status, result.Position, Id, task_item.TraceIdentifier);
                                }
                                else Logger?.LogWarningTaskOutcomeAlreadySet(Id, task_item.TraceIdentifier);
                            }
                        }
                        else {
                            #if TRACE
                            Logger?.LogTraceSessionProcessPendingTaskAlreadyCanceled(Id, task_item.TraceIdentifier);
                            #endif
                        }
                    }

                }
                finally {
                    CheckCompletion();
                }   
            }
            LogFinishBackgroundProcess();
            #if TRACE
            Logger?.LogTraceSessionProcessBkgEndedExit(Id);
            #endif

        }

        void SetProgress(TResult Result, Int32? EstimatedEnd)
        {
            #if TRACE
            Logger?.LogTraceSessionProcessCallback(Id);
            #endif
            try {
                CompletionToken.ThrowIfCancellationRequested();
            }
            catch(OperationCanceledException) {
                #if TRACE
                Logger?.LogTraceSessionProcessCallbackCanceled(Id);
                #endif
                throw;
            }
            lock(_lock) {
                #if TRACE
                Logger?.LogTraceSessionProcessCallbackLockAcquired(Id);
                #endif
                _result = Result;
                _estimatedEnd = EstimatedEnd;
                _progress++;
                SetStatus(RunnerStatus.Progressed);
                TaskListItem? task_item;
                Int32 task_position;
                //Complete tasks that wait for reaching this position by background 
                #if TRACE
                Logger?.LogTraceSessionProcessCallbackCompletePendingTasks(Id);
                #endif
                try {
                    while(_waitList.TryPeek(out task_item, out task_position) && (Status.IsFinal() || task_position <= _progress)) {
                        #if TRACE
                        Logger?.LogTraceSessionProcessCallbackCompleteAPendingTask(Id, task_item.TraceIdentifier);
                        #endif
                        task_item.Registration?.Dispose();
                        _waitList.Dequeue();
                        if(task_item.TaskSourceToComplete!=null) {
                            if(task_item.Token.IsCancellationRequested) {
                                #if TRACE
                                Logger?.LogTraceSessionProcessPendingTaskSetCanceled(Id, task_item.TraceIdentifier);
                                #endif
                                if(task_item.TaskSourceToComplete.TrySetCanceled())
                                    Logger?.LogDebugGetRequiredAsyncCanceled(Id, task_item.TraceIdentifier);
                                else Logger?.LogWarningTaskOutcomeAlreadySet(Id, task_item.TraceIdentifier);
                            }
                            else {
                                RunnerStatus old_status = Status;
                                Int32 old_position = Position;
                                RunnerResult<TResult> result = MakeResultAndAdjustState(_result, 
                                    task_position<_progress ? RunnerStatus.Progressed : RunnerStatus.Stalled, 
                                    task_position, 
                                    task_item.TraceIdentifier, 
                                    true);
                                #if TRACE
                                Logger?.LogTraceSessionProcessPendingTaskSetResult(Id, task_item.TraceIdentifier);
                                #endif
                                if(task_item.TaskSourceToComplete.TrySetResult(result)) {
                                    Position=task_position;
                                    Logger?.LogDebugSessionProcessRunnerResult(result.FailureException, result.Result?.ToString()??"<null>",
                                        result.Status, result.Position, Id, task_item.TraceIdentifier);
                                }
                                else {
                                    if(SetStatus(old_status)) //Will pass only if Status.IsRunning() is true
                                        Position = old_position;
                                    Logger?.LogWarningTaskOutcomeAlreadySet(Id, task_item.TraceIdentifier);
                                }
                            }
                        }
                        else {
                            #if TRACE
                            Logger?.LogTraceSessionProcessPendingTaskAlreadyCanceled(Id, task_item.TraceIdentifier);
                            #endif
                        }
                    }

                }
                finally {
                    CheckCompletion();
                }
            }
            #if TRACE
            Logger?.LogTraceSessionProcessCallbackExit(Id);
            #endif
        }

        RunnerResult<TResult> MakeResultAndAdjustState(TResult Result, RunnerStatus Status, Int32 Position, String TraceIdentifier, Boolean DelayCompletion)
        {
            #if TRACE
            Logger?.LogTraceSessionProcessResultTrySetNewStatus(Id, TraceIdentifier);  //TODO Rename logging method
            #endif
            Boolean status_changed =SetStatus(Status,true);
            if(status_changed) {
                if(Status==RunnerStatus.Failed) Exception=_backgroundException;
                #if TRACE
                Logger?.LogTraceSessionProcessResultNewStatusSet(Id, TraceIdentifier);  //TODO Rename logging method
                #endif
            }
            RunnerResult<TResult> result = new RunnerResult<TResult>(Result, this.Status, Position, Exception);
            this.Position=Math.Max(this.Position, Position);
            if(status_changed && !DelayCompletion) CheckCompletion();
            return result;
        }

        record TaskListItem
        {
            public TaskListItem() 
            {
                TraceIdentifier=ActiveSessionConstants.UNKNOWN_TRACE_IDENTIFIER;
            }
            public TaskCompletionSource<RunnerResult<TResult>>? TaskSourceToComplete;
            public CancellationToken Token;
            public CancellationTokenRegistration? Registration;
            public Int32 DesiredPosition;   
            public String TraceIdentifier; //Really not null
        }
    }
}
