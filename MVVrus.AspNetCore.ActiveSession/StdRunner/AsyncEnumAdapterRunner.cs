﻿using Microsoft.Extensions.Options;
using System.Collections;
using System.Collections.Concurrent;
using static MVVrus.AspNetCore.ActiveSession.RunnerStatus;
using static MVVrus.AspNetCore.ActiveSession.StdRunner.StdRunnerConstants;


namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public class AsyncEnumAdapterRunner<TItem> : EnumerableRunnerBase<TItem>
    {
        readonly Action<Task<bool>> _itemActionDelegate;

        readonly IAsyncEnumerable<TItem> _asyncSource;
        readonly bool _asyncEnumerableOwned; 

        IAsyncEnumerator<TItem> _asyncEnumerator = null!;
        volatile Context? _resultContext;
        volatile Task _taskChainTail;

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="AsyncSource"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public AsyncEnumAdapterRunner(IAsyncEnumerable<TItem> AsyncSource, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory) :
            this(AsyncSource, true, null, true, null, null, false, RunnerId, Options, LoggerFactory) { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="LoggerFactory"></param>
        [ActiveSessionConstructor]
        public AsyncEnumAdapterRunner(AsyncEnumAdapterParams<TItem> Params, RunnerId RunnerId, IOptionsSnapshot<ActiveSessionOptions> Options, ILoggerFactory? LoggerFactory): 
            this(Params.Source,Params.PassSourceOnership,Params.CompletionTokenSource,Params.PassCtsOwnership,
                Params.DefaultAdvance,Params.EnumAheadLimit, Params.StartInConstructor, RunnerId, Options, LoggerFactory) { }

        AsyncEnumAdapterRunner(
            IAsyncEnumerable<TItem> AsyncSource,
            Boolean PassSourceOnership,
            CancellationTokenSource? CompletionTokenSource,
            Boolean PassCtsOwnership,
            Int32? DefaultAdvance,
            Int32? EnumAheadLimit,
            Boolean StartInConstructor,
            RunnerId RunnerId,
            IOptionsSnapshot<ActiveSessionOptions> Options,
            ILoggerFactory? LoggerFactory):
            this(AsyncSource,PassSourceOnership,CompletionTokenSource,PassCtsOwnership,DefaultAdvance,EnumAheadLimit, 
                StartInConstructor, RunnerId, Options,
                LoggerFactory?.CreateLogger(Utilities.MakeClassCategoryName(typeof(AsyncEnumAdapterRunner<TItem>))))  { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="AsyncSource"></param>
        /// <param name="PassSourceOnership"></param>
        /// <param name="CompletionTokenSource"></param>
        /// <param name="PassCtsOwnership"></param>
        /// <param name="DefaultAdvance"></param>
        /// <param name="EnumAheadLimit"></param>
        /// <param name="StartInConstructor"></param>
        /// <param name="RunnerId"></param>
        /// <param name="Options"></param>
        /// <param name="Logger"></param>
        protected AsyncEnumAdapterRunner(
            IAsyncEnumerable<TItem> AsyncSource,
            Boolean PassSourceOnership,
            CancellationTokenSource? CompletionTokenSource,
            Boolean PassCtsOwnership,
            Int32? DefaultAdvance,
            Int32? EnumAheadLimit,
            Boolean StartInConstructor,
            RunnerId RunnerId,
            IOptionsSnapshot<ActiveSessionOptions> Options,
            ILogger? Logger) : base(CompletionTokenSource, PassCtsOwnership, RunnerId, Logger, Options, DefaultAdvance, EnumAheadLimit)
        {
            _asyncSource = AsyncSource;
            _taskChainTail = Task.CompletedTask;
            _itemActionDelegate = ItemAction;
            _asyncEnumerableOwned=PassSourceOnership;
             if(StartInConstructor) this.StartRunning();
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        protected override async Task DisposeAsyncCore()
        {
            Task task_chain_tail;
            DoAbort();
            do
            {
                task_chain_tail = _taskChainTail;
                await task_chain_tail;
            } while (_taskChainTail != task_chain_tail);
            if (_asyncEnumerator != null)
                await _asyncEnumerator.DisposeAsync();

            if (_asyncEnumerableOwned)
            {
                IAsyncDisposable? async_disposable = _asyncSource as IAsyncDisposable;
                if (async_disposable != null) await async_disposable.DisposeAsync();
                else (_asyncSource as IDisposable)?.Dispose();
            }
            await base.DisposeAsyncCore();
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected internal override void StartBackgroundExecution()
        {
                //Start _asyncEnumerable enumeration task chain
                _asyncEnumerator=_asyncSource.GetAsyncEnumerator(CompletionToken);
                _taskChainTail=_asyncEnumerator.MoveNextAsync().AsTask()
                    .ContinueWith(_itemActionDelegate, TaskContinuationOptions.RunContinuationsAsynchronously);
        }

        void ItemAction(Task<bool> NextStep)
        {
            bool proceed = false;
            bool result_ready;
            bool status_is_final;

            try
            {
                if (NextStep.IsCanceled) Abort();
                result_ready = status_is_final = Status.IsFinal();
                if (NextStep.IsFaulted) {
                    Exception = NextStep.Exception;
                    Queue.CompleteAdding();
                }
                if (NextStep.IsCompletedSuccessfully)  {
                    if (NextStep.Result && !status_is_final) {
                        Queue.Add(_asyncEnumerator.Current);
                        proceed = true;
                    }
                    else if (status_is_final) { //The queue may be legally disposed already, if so - eat the exception thrown due to this
                        try { Queue.CompleteAdding(); } catch (ObjectDisposedException) { }
                    }
                    else {
                        Queue.CompleteAdding();
                    }
                }
            }
            catch (Exception e)
            {
                Exception = e;
                Queue.CompleteAdding();
            }

            Context? result_context = _resultContext;
            if (result_context != null)
            {
                TItem? item;

                if(result_context.Token.IsCancellationRequested) {
                    _resultContext = null;
                    result_context.ResultTaskSource.SetCanceled();
                }

                while(result_context.Result.Count< result_context.MaxAdvance && Queue.TryTake(out item)) {
                    result_context.Result.Add(item);
                }

                if(result_context.Result.Count >= result_context.MaxAdvance) {
                    _resultContext = null;
                    result_context.ResultTaskSource.SetResult();
                }
            }

            if(proceed && !Disposed()) {
                _taskChainTail = _asyncEnumerator.MoveNextAsync().AsTask().ContinueWith(_itemActionDelegate);
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="MaxAdvance"></param>
        /// <param name="Result"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected internal override Task FetchRequiredAsync(Int32 MaxAdvance, List<TItem> Result, CancellationToken Token)
        {
            _resultContext = new Context(MaxAdvance, Result, Token);
            return _resultContext.ResultTaskSource.Task;
        }

        class Context
        {
            public int MaxAdvance { get; init; }
            public TaskCompletionSource ResultTaskSource { get; init; }
            public List<TItem> Result { get; init; }
            public CancellationToken Token { get; init; }


            public Context(int MaxAdvance, List<TItem> Result, CancellationToken Token)
            {
                ResultTaskSource = new TaskCompletionSource();
                this.MaxAdvance = MaxAdvance;
                this.Result = Result;
                this.Token = Token;
            }

        }
    }
}
