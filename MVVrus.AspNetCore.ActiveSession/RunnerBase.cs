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
        Int32 _state;
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
            _state=(Int32)RunnerState.NotStarted;
            _passCtsOwnership=PassCtsOwnership || CompletionTokenSource==null;
            _completionTokenSource=CompletionTokenSource??new CancellationTokenSource();
            CompletionToken = _completionTokenSource.Token;
            #if TRACE
            this.Logger?.LogTraceEnterRunnerBaseConstructorExit(RunnerId);
            #endif
        }

        /// <inheritdoc/>
        public virtual RunnerState State { get {return (RunnerState)Volatile.Read(ref _state);} }

        /// <inheritdoc/>
        public virtual Int32 Position { get; protected set; } = 0;

        /// <inheritdoc/>
        public void Abort() { 
            if(SetState(RunnerState.Aborted)) DoAbort();
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
        /// Sets the <see cref="State"/> property to the value specified by <paramref name="State"/> parameter in a thread-safe manner.
        /// If the runner is already in a final state, or a requested state value is NotStarted the state will not be changed 
        /// </summary>
        /// <param name="State">The target <see cref="State"/> property value</param>
        /// <returns> true if the <see cref="State"/> property</returns> value was really changed, false otherwise.
        protected virtual Boolean SetState(RunnerState State)
        {
            if (State==RunnerState.NotStarted) {
                #if TRACE
                Logger?.LogTraceRunnerBaseReturnToNotStartedStateAttempt(RunnerId);
                #endif
                return false;
            }
            Int32 new_state = (Int32)State, old_state;
            do {
                old_state=Volatile.Read(ref _state);
                if (((RunnerState)old_state).IsFinal()) {
                    #if TRACE
                    Logger?.LogTraceRunnerBaseChangeFinalStateAttempt(RunnerId);
                    #endif
                    return false;
                }
            } while (old_state!=Interlocked.CompareExchange(ref _state, new_state, old_state));
            #if TRACE
            Logger?.LogTraceRunnerBaseStateChanged(RunnerId, State);
            #endif
            if (((RunnerState)new_state).IsFinal())
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
        /// Set the runner's <see cref="State"/> property to a running state <paramref name="NewState"/> in a thread-safe manner
        /// only if the state was <see cref="RunnerState.NotStarted"/>
        /// </summary>
        /// <param name="NewState">The new <see cref="State"/> property value to be set.</param>
        /// <returns>true if <see cref="State"/> has been set, otherwise - false </returns>
        protected Boolean StartRunning(RunnerState NewState=RunnerState.Stalled)
        {
            CheckDisposed();
            RunnerState prev_state =
                (RunnerState)Interlocked.CompareExchange(ref _state, (int)NewState, (int)RunnerState.NotStarted);
            Boolean result = prev_state==RunnerState.NotStarted;
            #if TRACE
            Logger?.LogTraceRunnerBaseStartedInState(RunnerId, State);
            #endif
            if (result&&NewState.IsFinal()) {
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
            if (_disposed!=0) throw new ObjectDisposedException(this.GetType().FullName!);
        }

    }
}
