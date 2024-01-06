using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
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
        Int32 _disposed = 0;

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="CompletionTokenSource"></param>
        /// <param name="PassCtsOwnership"></param>
        public RunnerBase(CancellationTokenSource? CompletionTokenSource=null, Boolean PassCtsOwnership = true)
        {
            _passCtsOwnership=CompletionTokenSource==null? PassCtsOwnership: true;
            _completionTokenSource=CompletionTokenSource??new CancellationTokenSource();
            CompletionToken = _completionTokenSource.Token;
        }

        /// <inheritdoc/>
        public virtual RunnerState State { get {return (RunnerState)Volatile.Read(ref _state);} }

        /// <inheritdoc/>
        public virtual Int32 Position { get; protected set; }

        /// <inheritdoc/>
        public void Abort() { DoAbort(); }

        /// <inheritdoc/>
        public virtual CancellationToken CompletionToken { get; init; } 
        /// <summary>
        /// TODO
        /// </summary>
        public void Dispose()
        {
            if (SetDisposed()) {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="State"></param>
        protected virtual Boolean SetState(RunnerState State)
        {
            Int32 new_state = (Int32)State, old_state;
            do {
                old_state=Volatile.Read(ref _state);
                if (((RunnerState)old_state).IsFinal())
                    return false;
            } while (old_state!=Interlocked.CompareExchange(ref _state, new_state, old_state));
            if (((RunnerState)new_state).IsFinal())  _completionTokenSource?.Cancel();
            return true;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns>true if <see cref="State"/> was just changed to <see cref="RunnerState.Aborted"/>, false if final state was already set</returns>
        protected virtual Boolean DoAbort()
        {
            return SetState(RunnerState.Aborted);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="NewState"></param>
        /// <param name="OldState"></param>
        /// <returns></returns>
        protected RunnerState CompareAndSetStateInterlocked(RunnerState NewState, RunnerState OldState)
        {
            RunnerState prev_state =
                (RunnerState)Interlocked.CompareExchange(ref _state, (int)NewState, (int)OldState);
            if (NewState.IsFinal()&&!prev_state.IsFinal())
                _completionTokenSource?.Cancel();
            return prev_state;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Disposing"></param>
        protected virtual void Dispose(Boolean Disposing)
        {
            if(_passCtsOwnership) _completionTokenSource.Dispose();
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        protected Boolean SetDisposed()
        {
            return Interlocked.Exchange(ref _disposed, 1)==0;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        protected void CheckDisposed()
        {
            if (_disposed!=0) throw new ObjectDisposedException(this.GetType().FullName!);
        }

    }
}
