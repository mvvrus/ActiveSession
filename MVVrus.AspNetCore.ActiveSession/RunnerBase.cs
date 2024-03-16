using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;


//TODO Implement logging
namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// A class intended to be used as a base for <see cref="IRunner{TResult}"/> implementations
    /// </summary>
    public abstract class RunnerBase : IRunner, IDisposable
    {
        Int32 _status;
        readonly CancellationTokenSource _completionTokenSource;
        readonly Boolean _passCtsOwnership;
        Int32 _disposed = 0; //1 - the instance is disposed or to be disposed
        /// <value>
        /// <see cref="ILogger"/> instance used to write log messages. May be set via constructor or directly from the descendant class
        /// </value>
        protected ILogger? Logger { get; init; }

        /// <summary>
        /// A RunnerBase instance constructor
        /// </summary>
        /// <param name="CompletionTokenSource">
        /// External source for a value of the <see cref="CompletionToken"/> property. 
        /// If null (default value), a new <see cref="CancellationTokenSource"/> will be used as the source/
        /// </param>
        /// <param name="PassCtsOwnership">
        /// Should this instance be responsible for disposing an external <see cref="CompletionToken"/> source passed by <paramref name="CompletionTokenSource"/>.
        /// </param>
        /// <param name="RunnerId"><see cref="MVVrus.AspNetCore.ActiveSession.RunnerId"/> assigned to the runner to be created.</param>
        /// <param name="Logger"><see cref="ILogger"/> instance used to write log messages</param>
        /// <remarks>
        /// Because the class is intendend to be used as a base one, it's constructor has access level protected, not public.
        /// </remarks>
        protected RunnerBase(CancellationTokenSource? CompletionTokenSource, Boolean PassCtsOwnership, 
            RunnerId RunnerId=default, ILogger? Logger=null)
        {
            this.Logger=Logger;
            this.RunnerId=RunnerId;
            #if TRACE
            this.Logger?.LogTraceEnterRunnerBaseConstructor(RunnerId);
            #endif
            _status=(Int32)RunnerStatus.NotStarted;
            _passCtsOwnership=PassCtsOwnership || CompletionTokenSource==null;
            _completionTokenSource=CompletionTokenSource??new CancellationTokenSource();
            CompletionToken = _completionTokenSource.Token;
            #if TRACE
            this.Logger?.LogTraceEnterRunnerBaseConstructorExit(RunnerId);
            #endif
        }

        /// <inheritdoc/>
        public virtual RunnerStatus Status { get {return (RunnerStatus)Volatile.Read(ref _status);} }

        /// <inheritdoc/>
        public virtual Int32 Position { get; protected set; } = 0;

        /// <inheritdoc/>
        public void Abort() { 
            if(SetStatus(RunnerStatus.Aborted)) DoAbort();
        }

        /// <inheritdoc/>
        public virtual CancellationToken CompletionToken { get; init; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (SetDisposed()) {
                #if TRACE
                Logger?.LogTraceRunnerBaseDisposing(RunnerId);
                #endif
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <inheritdoc/>
        public Exception? Exception { get; protected set; }

        /// <inheritdoc/>
        public RunnerId RunnerId { get; init; }

        /// <summary>
        /// Sets the <see cref="Status"/> property to the value specified by <paramref name="Status"/> parameter in a thread-safe manner.
        /// If the runner is already in a final state, or a requested status value is NotStarted the status will not be changed 
        /// </summary>
        /// <param name="Status">The target <see cref="Status"/> property value</param>
        /// <returns> true if the <see cref="Status"/> property</returns> value was really changed, false otherwise.
        protected internal virtual Boolean SetStatus(RunnerStatus Status)
        {
            if (Status==RunnerStatus.NotStarted) {
                #if TRACE
                Logger?.LogTraceRunnerBaseReturnToNotStartedStateAttempt(RunnerId);
                #endif
                return false;
            }
            Int32 new_status = (Int32)Status, old_status;
            do {
                old_status=Volatile.Read(ref _status);
                if (((RunnerStatus)old_status).IsFinal()) {
                    #if TRACE
                    Logger?.LogTraceRunnerBaseChangeFinalStateAttempt(RunnerId);
                    #endif
                    return false;
                }
            } while (old_status!=Interlocked.CompareExchange(ref _status, new_status, old_status));
            #if TRACE
            Logger?.LogTraceRunnerBaseStateChanged(RunnerId, Status);
            #endif
            if (((RunnerStatus)new_status).IsFinal())
                try {
                    #if TRACE
                    Logger?.LogTraceRunnerBaseComeToFinalState(RunnerId);
                    #endif
                    _completionTokenSource?.Cancel();
                }
                catch (ObjectDisposedException) { }
            return true;
        }

        /// <summary>
        /// Semi-abstract method to perform additional work for the <see cref="Abort"/> method in descendent classes. Does nothing in this class
        /// </summary>
        protected virtual void DoAbort() {}

        /// <summary>
        /// Abstract Method to start background execution synchronously.  Must be overriden in descendent classes.
        /// </summary>
        /// <exception cref="Exception">A specific descendant of this class thrown in the case of failure starting the execution</exception>
        /// <remarks>This base method just throws <see cref="NotImplementedException"/> and should never be called in its overrides</remarks>
        protected internal abstract void StartBackgroundExecution();

        /// <summary>
        /// A method intendent to start background execution asynchronously in descendent classes. 
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous starting process and its outcome</returns>
        /// <remarks> This default implementation is a stub. It just starts the a background execution synchronously and returns a completed task </remarks>
        protected internal virtual Task StartBackgroundExecutionAsync()
        {
            StartBackgroundExecution();
            return Task.CompletedTask;
        }


        /// <summary>
        /// Begin (and possibly finish immediatly) background execution of the runner if it is not started, and set Sta
        /// Set the runner's <see cref="Status"/> property accordingly.
        /// </summary>
        /// <param name="NewStatus">The new <see cref="Status"/> property value to be set.</param>
        /// <returns>true the runner status has been really changed, otherwise - false </returns>
        /// <remarks>
        /// Sets the runner's <see cref="Status"/> property  to a running status <paramref name="NewStatus"/> in a 
        /// thread-safe manner (only if the status was <see cref="RunnerStatus.NotStarted"/>)
        /// and tries to start a backgrund processing via a <see cref="StartBackgroundExecution"/> virtual method
        /// </remarks>
        protected internal Boolean StartRunning(RunnerStatus NewStatus=RunnerStatus.Stalled)
        {
            CheckDisposed();
            RunnerStatus prev_status =
                (RunnerStatus)Interlocked.CompareExchange(ref _status, (int)NewStatus, (int)RunnerStatus.NotStarted);
            Boolean result = prev_status==RunnerStatus.NotStarted;
            #if TRACE
            Logger?.LogTraceRunnerBaseStartedInState(RunnerId, Status); //TODO if(result)?
            #endif
            if (result) try 
                {
                    StartBackgroundExecution();
                }
                catch (Exception exception) {
                    Logger?.LogErrorStartBkgProcessingFailed(exception, RunnerId);
                    FailStartRunning(NewStatus);
                    throw;
                }
            if (result&&NewStatus.IsFinal()) {
                #if TRACE
                Logger?.LogTraceRunnerBaseComeToFinalState(RunnerId);
                #endif
                _completionTokenSource?.Cancel();
            }
            return result; 
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="NewStatus"></param>
        /// <returns></returns>
        protected internal async Task<Boolean> StartRunningAsync(RunnerStatus NewStatus = RunnerStatus.Stalled)
        {
            CheckDisposed();
            RunnerStatus prev_status =
                (RunnerStatus)Interlocked.CompareExchange(ref _status, (int)NewStatus, (int)RunnerStatus.NotStarted);
            Boolean result = prev_status == RunnerStatus.NotStarted;
            #if TRACE
            Logger?.LogTraceRunnerBaseStartedInState(RunnerId, Status);   //TODO Use anover logger method?
            #endif
            if(result) try {
                    await StartBackgroundExecutionAsync();
                }
                catch(Exception exception) {
                    Logger?.LogErrorStartBkgProcessingFailed(exception, RunnerId);
                    FailStartRunning(NewStatus);
                    throw;
                }
            if(result && NewStatus.IsFinal()) {
                #if TRACE
                Logger?.LogTraceRunnerBaseComeToFinalState(RunnerId);
                #endif
                _completionTokenSource?.Cancel();
            }
            return result;

        }


        /// <summary>
        /// Rollback value of the <see cref="Status"/> property in the case of unsuccessful start of background execution
        /// </summary>
        /// <param name="FromNewStatus">Expected value from wich the property to be changed to <see cref="RunnerStatus.NotStarted"/></param>
        protected internal void FailStartRunning(RunnerStatus FromNewStatus)
        {
            RunnerStatus rolled_back = (RunnerStatus)Interlocked.Exchange(ref _status, (int)RunnerStatus.NotStarted);
            if(rolled_back!=FromNewStatus) Logger?.LogWarningUnexpectedStatusChange(RunnerId, FromNewStatus, rolled_back);
        }

        //protected inetrnal Boolean SetStartRunningStatus(RunnerStatusExtensions)

        /// <summary>
        /// This virtual method will be called from <see cref="SetDisposed"/> method when the object 
        /// realy transition ito the <see cref="Disposed"/> state
        /// </summary>
        protected virtual void PreDispose() {}

        /// <summary>
        /// This method is a part of standard disposable pattern. It performs a real work disposing the instance
        /// </summary>
        /// <param name="Disposing">Flag that the metod is called from Dispose(). Not used in this base class/</param>
        protected virtual void Dispose(Boolean Disposing)
        {
            if(_passCtsOwnership) _completionTokenSource.Dispose();
        }

        /// <summary>
        /// Change state of the object to disposed.
        /// Sets the _disposed flag in a thread-safe manner
        /// </summary>
        /// <returns>true if the state just has been changed to disposing/disposed</returns>
        /// <remarks>
        /// This method is called at the beginning of <see cref="IDisposable.Dispose"/> method. 
        /// It also MUST be called at the beginning of <see cref="IAsyncDisposable.DisposeAsync"/> of the all descendent classes
        /// that implements interface <see cref="IAsyncDisposable"/>
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
        /// Checks that the instance is not disposed. Throws <exception cref="ObjectDisposedException"></exception> otherwise. 
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        protected void CheckDisposed()
        {
            if (Disposed()) throw new ObjectDisposedException(DisposedObjectName());
        }

        /// <summary>
        /// Returns object name for an <see cref="ObjectDisposedException"/> constructor
        /// </summary>
        /// <returns>Return string with the object class name</returns>
        /// <remarks>This method is intended to make exception message to be more uniform</remarks>
        protected String DisposedObjectName()
        {
            String name= GetType().Name;
            int pos = name.IndexOf('`');
            return pos>=0?name.Substring(0,pos):name;
        }

        /// <summary>
        /// Allow descendant classes to check if disposing has been started
        /// </summary>
        /// <returns></returns>
        protected Boolean Disposed()
        {
            return Volatile.Read(ref _disposed)!=0;
        }

    }
}
