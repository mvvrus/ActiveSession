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
    public class RunnerBase : IRunner, IDisposable
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
        protected virtual Boolean SetStatus(RunnerStatus Status)
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
        /// Set the runner's <see cref="Status"/> property to a running status <paramref name="NewStatus"/> in a thread-safe manner
        /// only if the status was <see cref="RunnerStatus.NotStarted"/>
        /// </summary>
        /// <param name="NewStatus">The new <see cref="Status"/> property value to be set.</param>
        /// <returns>true if <see cref="Status"/> has been set, otherwise - false </returns>
        protected Boolean StartRunning(RunnerStatus NewStatus=RunnerStatus.Stalled)
        {
            CheckDisposed();
            RunnerStatus prev_status =
                (RunnerStatus)Interlocked.CompareExchange(ref _status, (int)NewStatus, (int)RunnerStatus.NotStarted);
            Boolean result = prev_status==RunnerStatus.NotStarted;
            #if TRACE
            Logger?.LogTraceRunnerBaseStartedInState(RunnerId, Status);
            #endif
            if (result&&NewStatus.IsFinal()) {
                #if TRACE
                Logger?.LogTraceRunnerBaseComeToFinalState(RunnerId);
                #endif
                _completionTokenSource?.Cancel();
            }
            return result; 
        }

        /// <summary>
        /// This method is a part of standard disposable pattern. It performs a real work disposing the instance
        /// </summary>
        /// <param name="Disposing">Flag that the metod is called from Dispose(). Not used in this base class/</param>
        protected virtual void Dispose(Boolean Disposing)
        {
            if(_passCtsOwnership) _completionTokenSource.Dispose();
        }

        /// <summary>
        /// Sets the _disposed flag in a thread-safe manner
        /// </summary>
        /// <returns></returns>
        protected Boolean SetDisposed()
        {
            return Interlocked.Exchange(ref _disposed, 1)==0;
        }

        /// <summary>
        /// Checks that the instance is not disposed. Throws <exception cref="ObjectDisposedException"></exception> otherwise. 
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        protected void CheckDisposed()
        {
            if (Disposed()) throw new ObjectDisposedException(this.GetType().FullName!);
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
