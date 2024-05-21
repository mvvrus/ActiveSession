using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;


namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// An abstract class intended to be used as a base for runner classes implementing <see cref="IRunner{TResult}"/>
    /// </summary>
    public abstract class RunnerBase : IRunner, IDisposable
    {
        Int32 _status;
        readonly CancellationTokenSource _completionTokenSource;
        readonly Boolean _passCtsOwnership;
        Int32 _disposed = 0; //1 - the instance is disposed or to be disposed
        /// <summary>
        /// An <see cref="ILogger"/> instance used to write log messages from a runner. 
        /// </summary>
        protected ILogger? Logger { get; init; }

        /// <summary>
        /// Protected. A RunnerBase instance constructor intended to be used by decendent classes constructors. 
        /// </summary>
        /// <param name="CompletionTokenSource">
        /// External source for a value of the <see cref="RunnerBase.CompletionToken"/> property. If it is null (default value), 
        /// a new <see cref="CancellationTokenSource"/> will be used as the source in the instance to be created.
        /// </param>
        /// <param name="PassCtsOwnership">
        /// This value indicates will the instance to be created be responsible for disposing an external <see cref="CompletionToken"/> source if it was passed by <paramref name="CompletionTokenSource"/>.
        /// </param>
        /// <param name="RunnerId">The identifier to be assigned to the instance to be created.</param>
        /// <param name="Logger">The <see cref="ILogger"/> instance used to write log messages from the instance to be created.</param>
        /// <remarks>
        /// Because the class is intendend to be used as a base for deescendent classes, 
        /// it's constructor has access level protected, not public.
        /// </remarks>
        protected RunnerBase(CancellationTokenSource? CompletionTokenSource, Boolean PassCtsOwnership, 
            RunnerId RunnerId=default, ILogger? Logger=null)
        {
            this.Logger=Logger;
            this.Id=RunnerId;
            #if TRACE
            this.Logger?.LogTraceRunnerBaseConstructorEnter(RunnerId);
            #endif
            _status=(Int32)RunnerStatus.NotStarted;
            _passCtsOwnership=PassCtsOwnership || CompletionTokenSource==null;
            _completionTokenSource=CompletionTokenSource??new CancellationTokenSource();
            CompletionToken = _completionTokenSource.Token;
            #if TRACE
            this.Logger?.LogTraceRunnerBaseConstructorExit(RunnerId);
            #endif
        }

        ///<summary>
        /// Virtual. <toinherit><inheritdoc path="/summary/node()"/> It is a part of <see cref="IRunner"/> interface implementation.</toinherit>
        ///</summary>
        /// <inheritdoc/>
        public virtual RunnerStatus Status { get {return (RunnerStatus)Volatile.Read(ref _status);} }

        ///<summary>
        /// Virtual. <toinherit><inheritdoc path="/summary/node()"/> It is a part of <see cref="IRunner"/> interface implementation.</toinherit>
        ///</summary>
        /// <inheritdoc/>
        public virtual Int32 Position { get; protected set; } = 0;

        ///<summary>
        /// <inheritdoc/> It is a part of <see cref="IRunner"/> interface implementation.
        ///</summary>
        ///<remarks>
        /// <inheritdoc/>
        ///The method is intended to be safe for calling even if the runner has been disposed already.
        ///</remarks>
        /// <inheritdoc/>
        public void Abort(String? TraceIdentifier = null) 
        {
            if(SetStatus(RunnerStatus.Aborted)) {
                Logger?.LogTraceRunnerBaseAbortCalled(Id, TraceIdentifier ?? UNKNOWN_TRACE_IDENTIFIER);
                DoAbort(TraceIdentifier ?? UNKNOWN_TRACE_IDENTIFIER);
            }
        }

        ///<summary>
        /// <inheritdoc/> It is a part of <see cref="IRunner"/> interface implementation.
        ///</summary>
        /// <inheritdoc/>
        public CancellationToken CompletionToken { get; init; }

        ///<summary>
        /// <inheritdoc/> 
        /// It is a part of <see cref="IDisposable"/> interface implementation.
        ///</summary>
        /// <inheritdoc/>
        public void Dispose()
        {
            if (SetDisposed()) {
                #if TRACE
                Logger?.LogTraceRunnerBaseDisposing(Id);
                #endif
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        ///<summary>
        /// <inheritdoc/> It is a part of <see cref="IRunner"/> interface implementation.
        ///</summary>
        /// <inheritdoc/>
        public Exception? Exception { get; protected set; }

        ///<summary>
        /// <inheritdoc/> It is a part of <see cref="IRunner"/> interface implementation.
        ///</summary>
        /// <inheritdoc/>
        public RunnerId Id { get; init; }

        /// <inheritdoc/>
        public abstract Boolean IsBackgroundExecutionCompleted { get; }

        /// <inheritdoc/>
        public abstract RunnerBkgProgress GetProgress();

        /// <summary>
        /// Protected. Sets the <see cref="Status"/> property enforcing rules for changing this property.
        /// </summary>
        /// <param name="Status">The new value for the <see cref="Status"/> property.</param>
        /// <returns> 
        /// <see langword="true"/> if the <see cref="Status"/> property value has really been changed, <see langword="false"/>  otherwise.
        /// </returns>
        /// <remarks>
        /// The enforced rules rules are the following:
        /// <list type="number">
        /// <item><description>
        /// The <see cref="Status"/> value of <see cref="RunnerStatus.NotStarted">NotStarted</see> cannot be changed.
        /// I.e. this method cannot be used to start a background execution of a runner. 
        /// Use <see cref="StartRunning(RunnerStatus)"/> or <see cref="StartRunningAsync(RunnerStatus)"/> instead.
        /// </description></item>
        /// <item><description>If the <see cref="Status"/> value is a final one it will not be changed.</description></item>
        /// <item><description>
        /// The <see cref="Status"/> value cannot be set to <see cref="RunnerStatus.NotStarted">NotStarted</see>.
        /// </description></item>
        /// </list>
        /// The value of the property is set in a thread-safe manner. 
        /// If the value of the property become a final one the <see cref="CompletionToken"/> will go to Canceled state.
        /// </remarks>
        protected internal Boolean SetStatus(RunnerStatus Status)
        {
            if (Status==RunnerStatus.NotStarted) {
                #if TRACE
                Logger?.LogTraceRunnerBaseReturnToNotStartedStateAttempt(Id);
                #endif
                return false;
            }
            Int32 new_status = (Int32)Status, old_status;
            do {
                old_status=Volatile.Read(ref _status);
                if (((RunnerStatus)old_status).IsFinal()) {
                    #if TRACE
                    Logger?.LogTraceRunnerBaseChangeFinalStateAttempt(Id);
                    #endif
                    return false;
                }
            } while (old_status!=Interlocked.CompareExchange(ref _status, new_status, old_status));
            #if TRACE
            Logger?.LogTraceRunnerBaseStateChanged(Id, Status);
            #endif
            if (((RunnerStatus)new_status).IsFinal())
                try {
                    #if TRACE
                    Logger?.LogTraceRunnerBaseComeToFinalState(Id);
                    #endif
                    _completionTokenSource?.Cancel();
                }
                catch (ObjectDisposedException) { }
            return true;
        }

        /// <summary>
        /// Protected virtual. 
        /// <toinherit>This method performs and additional work for implementing the <see cref="Abort"/> method. </toinherit>
        /// It is intended to be overriden in descendent classes.
        /// </summary>
        /// <param name="TraceIdentifier">
        /// <inheritdoc cref="IRunner.Abort(string?)"/>
        /// </param>
        /// <remarks> This method in this particular class is semi-abstract, i.e it  does nothing.</remarks>
        protected virtual void DoAbort(String TraceIdentifier) {}

        /// <summary>
        /// Protected abstract. <toinherit>This method is used to start background execution synchronously.</toinherit>  
        /// </summary>
        /// <remarks>Must be overriden in descendent classes. <toinherit></toinherit></remarks>
        protected internal abstract void StartBackgroundExecution();

        /// <summary>
        /// Protected virtual. <toinherit>This method intended to start the background execution asynchronously.</toinherit>
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous starting process and its outcome. </returns>
        /// <remarks> 
        /// This default implementation is a stub. It just starts the a background execution 
        /// synchronously and returns a completed task. This method should be overriden in descendent classes, 
        /// that supports real asynchronous start of a background execution.<toinherit></toinherit>
        /// </remarks>
        protected internal virtual Task StartBackgroundExecutionAsync()
        {
            StartBackgroundExecution();
            return Task.CompletedTask;
        }


        /// <summary>
        /// Protected. Begins background execution of the runner if it is not started, 
        /// and in that case sets the runner's <see cref="Status"/> property to the specified value.
        /// </summary>
        /// <param name="NewStatus">The new <see cref="Status"/> property value to be set.</param>
        /// <returns><see langword="true"/> the runner status has been really changed, otherwise - <see langword="false"/>  </returns>
        /// <remarks>
        /// If the runner's <see cref="Status"/> property value is changed to the <paramref name="NewStatus"/> value 
        /// it will occur in a  thread-safe manner. 
        /// A change of the <see cref="Status"/> property value occures only 
        /// if the original status was <see cref="RunnerStatus.NotStarted"/>).
        /// If this is the case the the method tries to start a backgrund processing via a <see cref="StartBackgroundExecution"/> 
        /// virtual method. If the  <see cref="Status"/> property value is changes to a final one the runner should stop 
        /// its background execution
        /// </remarks>
        protected internal Boolean StartRunning(RunnerStatus NewStatus=RunnerStatus.Stalled)
        {
            CheckDisposed();
            RunnerStatus prev_status =
                (RunnerStatus)Interlocked.CompareExchange(ref _status, (int)NewStatus, (int)RunnerStatus.NotStarted);
            Boolean result = prev_status==RunnerStatus.NotStarted;
            #if TRACE
            Logger?.LogTraceRunnerBaseStartedInState(Id, Status); 
            #endif
            if (result) try 
                {
                    StartBackgroundExecution();
                }
                catch (Exception exception) {
                    Logger?.LogErrorStartBkgProcessingFailed(exception, Id);
                    FailStartRunning(NewStatus);
                    throw;
                }
            if (result&&NewStatus.IsFinal()) {
                #if TRACE
                Logger?.LogTraceRunnerBaseComeToFinalState(Id);
                #endif
                _completionTokenSource?.Cancel();
            }
            return result; 
        }

        /// <summary>
        /// Protected. Asynchronously begins background execution of the runner if it has not been started yet, 
        /// and in that case sets the runner's <see cref="Status"/> property to the specified value.
        /// </summary>
        /// <param name="NewStatus">The new <see cref="Status"/> property value to be set.</param>
        /// <returns>
        /// The task that is used to observe an outcome of an asynchronous start of a background execution.
        /// If the task completes successfully its result will be set according to the same rules 
        /// as a result of <see cref="StartRunning(RunnerStatus)">StartRunning</see> method.
        /// </returns>
        protected internal async Task<Boolean> StartRunningAsync(RunnerStatus NewStatus = RunnerStatus.Stalled)
        {
            CheckDisposed();
            RunnerStatus prev_status =
                (RunnerStatus)Interlocked.CompareExchange(ref _status, (int)NewStatus, (int)RunnerStatus.NotStarted);
            Boolean result = prev_status == RunnerStatus.NotStarted;
            #if TRACE
            Logger?.LogTraceRunnerBaseStartedInState(Id, Status);   //TODO Use anover logger method?
            #endif
            if(result) try {
                    await StartBackgroundExecutionAsync();
                }
                catch(Exception exception) {
                    Logger?.LogErrorStartBkgProcessingFailed(exception, Id);
                    FailStartRunning(NewStatus);
                    throw;
                }
            if(result && NewStatus.IsFinal()) {
                #if TRACE
                Logger?.LogTraceRunnerBaseComeToFinalState(Id);
                #endif
                _completionTokenSource?.Cancel();
            }
            return result;

        }


        /// <summary>
        /// Protected. Rolls back the value of the <see cref="Status"/> property 
        /// in the case of unsuccessful start of background execution.
        /// </summary>
        /// <param name="FromNewStatus">Expected value from wich the property to be changed to <see cref="RunnerStatus.NotStarted"/></param>
        /// <remarks>The value of the <see cref="Status"/> property is changed in a thread-safe manner</remarks>
        protected internal void FailStartRunning(RunnerStatus FromNewStatus)
        {
            RunnerStatus rolled_back = (RunnerStatus)Interlocked.Exchange(ref _status, (int)RunnerStatus.NotStarted);
            if(rolled_back!=FromNewStatus) Logger?.LogWarningUnexpectedStatusChange(Id, FromNewStatus, rolled_back);
        }

        /// <summary>
        /// Protected virtual. 
        /// <toinherit>Performs preliminary tasks before beginning of disposing any members of any descendent class.</toinherit>
        /// </summary>
        /// <remarks>
        /// <toinherit>
        /// This virtual method is called from <see cref="SetDisposed"/> method when the object realy transitions into the <see cref="Disposed"/> state.
        /// Is intended to be overriden in descendent classes. 
        /// </toinherit>
        /// In this particular class the method is defined as semi-abstract, i.e. it does nothing.
        /// </remarks>
        protected virtual void PreDispose() {}

        /// <summary>
        /// Protected virtual. <toinherit>This method performs a real work of disposing the object instance (synchronously).</toinherit>
        /// </summary>
        /// <param name="Disposing">Flag that the metod is called from Dispose().</param>
        /// <remarks>  
        /// <toinherit>
        /// The parameter <paramref name="Disposing"/> is uses according to disposing pattern to distinguish calls 
        /// from <see cref="IDisposable.Dispose"/> methhod from calls from a finalizer.
        /// </toinherit>
        /// <nofinalizer>
        /// Because this particular class does not have a finalizer, the parameter is of no use.
        /// </nofinalizer>
        /// </remarks>
        protected virtual void Dispose(Boolean Disposing)
        {
            if(_passCtsOwnership) _completionTokenSource.Dispose();
        }

        /// <summary>
        /// Protected. Changes state of the object to flag that its disposing is started (synchronously or asynchronously).
        /// </summary>
        /// <returns><see langword="true"/> if the state just has been changed to disposing/disposed, otherwise <see langword="false"/></returns>
        /// <remarks>
        /// The state is changed in a thread-safe manner.
        /// This method is called at the beginning of <see cref="IDisposable.Dispose"/> method. 
        /// It also MUST be called at the beginning of <see cref="IAsyncDisposable.DisposeAsync"/> method 
        /// in all descendent classes that implements interface <see cref="IAsyncDisposable"/>
        /// </remarks>
        protected Boolean SetDisposed()
        {
            if(Interlocked.Exchange(ref _disposed, 1) == 0) {
                PreDispose();
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Protected. Checks that the instance is not disposed. Throws <exception cref="ObjectDisposedException"></exception> otherwise. 
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <remarks> This method is intended to be used by descendent classes to avoid operations in a disposed state.</remarks>
        protected void CheckDisposed()
        {
            if (Disposed()) throw new ObjectDisposedException(DisposedObjectName());
        }

        /// <summary>
        /// Protected. Returns object name for an <see cref="ObjectDisposedException"/> to be thrown
        /// </summary>
        /// <returns>Return string with the object class name</returns>
        /// <remarks>This method is intended to be used in descendent classes to make 
        /// <see cref="ObjectDisposedException"/> exception messages to be more uniform</remarks>
        protected String DisposedObjectName()
        {
            String name= GetType().Name;
            int pos = name.IndexOf('`');
            return pos>=0?name.Substring(0,pos):name;
        }

        /// <summary>
        /// Protected. Allows to check if the disposing this runner instance has been at least started already.
        /// </summary>
        /// <returns><see langword="true"/> if the diposing has been started, <see langword="false"/> otherwise.</returns>
        protected Boolean Disposed()
        {
            return Volatile.Read(ref _disposed)!=0;
        }

    }
}
