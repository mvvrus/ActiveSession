using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;

namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// TODO write documentation
    /// </summary>
    public abstract class ActiveSessionRunnerBase: IActiveSessionRunner
    {
        Int32 _state;

        /// <inheritdoc/>
        public virtual ActiveSessionRunnerState State { 
            get
            {
                CheckDisposed();
                return (ActiveSessionRunnerState)Volatile.Read(ref _state);
            }
        }


        /// <inheritdoc/>
        public virtual Int32 Position { get; protected set; }

        /// <inheritdoc/>
        public abstract void Abort();

        /// <inheritdoc/>
        public virtual CancellationToken GetCompletionToken()
        {
            return CancellationToken.None; //TODO Imlement this method in conjecuntion with Abort method
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected Boolean _disposed=false;

        /// <summary>
        /// TODO
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        protected void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(this.GetType().FullName!);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="State"></param>
        protected void SetState(ActiveSessionRunnerState State)
        {
            _state = (int)State;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="State"></param>
        /// <returns></returns>
        protected ActiveSessionRunnerState SetStateInterlocked(ActiveSessionRunnerState State)
        {
            return (ActiveSessionRunnerState)Interlocked.Exchange(ref _state,(int)State);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="State"></param>
        /// <param name="OldState"></param>
        /// <returns></returns>
        protected ActiveSessionRunnerState CompareAndSetStateInterlocked(ActiveSessionRunnerState State, ActiveSessionRunnerState OldState)
        {
            return (ActiveSessionRunnerState)Interlocked.CompareExchange(ref _state, (int)State, (int) OldState);
        }
    }
}
