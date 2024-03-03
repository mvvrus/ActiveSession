using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using static MVVrus.AspNetCore.ActiveSession.RunnerStatus;
using static MVVrus.AspNetCore.ActiveSession.StdRunner.StdRunnerConstants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// This adapter class makes possible to use any class implementing <see cref="IEnumerable{T}"/>interface as an ActiveSession runner
    /// </summary>
    public class EnumAdapterRunner<TItem> : EnumerableRunnerBase<TItem>, ICriticalNotifyCompletion
    {
        //TODO Implement logging

        IEnumerable<TItem>? _source;
        Boolean _passSourceOwnership;
        Task? _enumTask;

        internal Task? EnumTask { get => _enumTask; }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public EnumAdapterRunner(IEnumerable<TItem> Source, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options,
            ILoggerFactory? LoggerFactory) :
            this(Source,true,null,true,null,null,false,RunnerId, Options, LoggerFactory) { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public EnumAdapterRunner(EnumAdapterParams<TItem> Params, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory) :
            this(Params.Source, Params.PassSourceOnership, Params.CompletionTokenSource, Params.PassCtsOwnership, 
                Params.DefaultAdvance, Params.EnumAheadLimit, Params.StartInConstructor, RunnerId, Options, LoggerFactory) {}

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="PassSourceOnership"></param>
        /// <param name="CompletionTokenSource"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <param name="DefaultAdvance"></param>
        /// <param name="EnumAheadLimit"></param>
        /// <param name="StartInConstructor"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
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
            ILoggerFactory? LoggerFactory) :
            base(CompletionTokenSource, PassCtsOwnership, RunnerId,
                LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(typeof(EnumAdapterRunner<TItem>))),
                Options, DefaultAdvance, EnumAheadLimit)
        {
            _source = Source ?? throw new ArgumentNullException(nameof(Source));
            _passSourceOwnership = PassSourceOnership;
#if TRACE
#endif
            //TODO LogDebug parameters passed

            _runAwaitContinuationDelegate = RunAwaitContinuation;
            if (StartInConstructor) StartBackgroundProcessing();
#if TRACE
#endif
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        protected async override Task DisposeAsyncCore()
        {
            if (_enumTask != null) await _enumTask!;
            await base.DisposeAsyncCore();
        }


        void ReleaseSource()
        {
#if TRACE
#endif
            IDisposable? base_disposable = _passSourceOwnership ? Interlocked.Exchange(ref _source, null) as IDisposable : null;
            base_disposable?.Dispose();
            //TODO
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected internal override void StartBackgroundProcessing()
        {
#if TRACE
#endif
            _enumTask = Task.Run(EnumerateSource);
#if TRACE
#endif
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="MaxAdvance"></param>
        /// <param name="Result"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        protected internal override async Task FetchRequiredAsync(Int32 MaxAdvance, List<TItem> Result, CancellationToken Token)
        {
            for(int i = Result.Count; i < MaxAdvance && Status.IsRunning(); i++) {
                Token.ThrowIfCancellationRequested();

                TItem? item;
                if(Queue.TryTake(out item)) {
#if TRACE
#endif
                    Result.Add(item ?? throw new NullReferenceException());
                }
                else if(Queue.IsAddingCompleted) {
#if TRACE
#endif
                    break;
                }
                else {
#if TRACE
#endif
                    await this;
                }
            }

        }

        void EnumerateSource()
        {
            CancellationToken completion_token = CompletionToken;
            if (_source == null) return;
            try
            {
                foreach(TItem item in _source!) {
#if TRACE
#endif              
                    if (completion_token.IsCancellationRequested) {
#if TRACE
#endif
                        break;
                    }
                    if (Status.IsFinal())
                    {  
#if TRACE
#endif
                        break;
                    }
                    if (Queue.TryAdd(item, -1, completion_token))
                    {
#if TRACE
#endif
                        TryRunAwaitContinuation();
                    }
                    else if (completion_token.IsCancellationRequested) { 
                        //TODO Are we really so need to avoid one more enumeration
#if TRACE
#endif
                        break;
                    }
                }
#if TRACE
#endif
            }
            catch (Exception e)
            {
                //LogError
                Exception = e;
            }
            finally
            {
#if TRACE
#endif
                Queue.CompleteAdding();
                ReleaseSource();
                TryRunAwaitContinuation();
            }
#if TRACE
#endif
        }

        #region Await stuff
        readonly Action<Action> _runAwaitContinuationDelegate;
        Action? _continuation;
        readonly ManualResetEventSlim _complete_event = new ManualResetEventSlim(true);

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        public EnumAdapterRunner<TItem> GetAwaiter() { return this; }
        /// <summary>
        /// TODO
        /// </summary>
        public bool IsCompleted { get { return Queue.Count > 0; } }
        /// <summary>
        /// TODO
        /// </summary>
        public void GetResult() { _complete_event.Wait(); }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="continuation"></param>
        public void OnCompleted(Action continuation)
        {
            Schedule(continuation);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="continuation"></param>
        public void UnsafeOnCompleted(Action continuation)
        {
            Schedule(continuation);
        }

        private void Schedule(Action continuation)
        {
            _complete_event.Reset();
            try
            {
                if (Interlocked.CompareExchange(ref _continuation, continuation, null) != null)
                {
                    ThrowInvalidParallelism();
                }
            }
            catch
            {
#if TRACE
#endif
                _complete_event.Set();
                //TODO the reaction for an error
            }
            if (Queue.IsAddingCompleted)
            {
#if TRACE
#endif
                TryRunAwaitContinuation();
            }
#if TRACE
#endif
        }

        void TryRunAwaitContinuation()
        {
#if TRACE
#endif
            Action? continuation = Interlocked.Exchange(ref _continuation, null);
            if (continuation != null)
            {
#if TRACE
#endif
                ThreadPool.QueueUserWorkItem(_runAwaitContinuationDelegate, continuation, false);
            }
#if TRACE
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
