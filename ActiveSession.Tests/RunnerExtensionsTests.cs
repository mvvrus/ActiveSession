using System.Linq.Expressions;

namespace ActiveSession.Tests
{
    public class RunnerExtensionsTests
    {
        const Int32 TIMEOUT=5000;

        [Fact]
        //Test group: IsLocal method
        public void IsLocalTest()
        {
            TestSetupIsLocal ts=new TestSetupIsLocal();
            //Test case: IRunnerProxy not implemented
            ts.VerifyNoProxy();
            //Test case: IRunnerProxy implemented, executing locally
            ts.VerifyLocalProxy();
            //Test case: IRunnerProxy implemented, executing remotely
            ts.VerifyRemoteProxy();
        }

        [Fact]
        //Test group: test RunExtensions.GetStatusAsync
        public void StatusAsyncTest()
        {
            using(TestSetupNoParams<RunnerStatus> ts=new (RunnerStatus.Completed
                , RunnerStatus.Aborted
                , s => s.Status
                , s => s.GetStatusAsync(It.IsAny<CancellationToken>())
                , (s, c) => RunnerExtensions.GetStatusAsync(s, c))
            ) {
                //Test case: IRunnerProxy not implemented
                ts.VerifyNoProxy();
                //Test case: IRunnerProxy implemented, executed locally
                ts.VerifyLocalProxy();
                //Test case: IRunnerProxy implemented, executed remotely and ran to completion
                ts.VerifyRemoteProxyCompleted();
                //Test case: IRunnerProxy implemented, executed remotely and cancelled
                ts.VerifyRemoteProxyCanceled();
            }
        }

        [Fact]
        //Test group: test RunExtensions.GetPositionAsync
        public void PositionAsyncTest()
        {
            using(TestSetupNoParams<Int32> ts = new(1   
                , 42
                , s => s.Position
                , s => s.GetPositionAsync(It.IsAny<CancellationToken>()),
                (s, c) => RunnerExtensions.GetPositionAsync(s, c))
            ) {
                //Test case: IRunnerProxy not implemented
                ts.VerifyNoProxy();
                //Test case: IRunnerProxy implemented, executed locally
                ts.VerifyLocalProxy();
                //Test case: IRunnerProxy implemented, executed remotely and ran to completion
                ts.VerifyRemoteProxyCompleted();
                //Test case: IRunnerProxy implemented, executed remotely and cancelled
                ts.VerifyRemoteProxyCanceled();
            }
        }

        [Fact]
        //Test group: test RunExtensions.GetExceptionAsync
        public void ExceptionAsyncTest()
        {
            using(TestSetupNoParams<Exception?> ts = new(new Exception("1")
                , new Exception("2")
                , s => s.Exception
                , s => s.GetExceptionAsync(It.IsAny<CancellationToken>()),
                (s, c) => RunnerExtensions.GetExceptionAsync(s, c))
            ) {
                //Test case: IRunnerProxy not implemented
                ts.VerifyNoProxy();
                //Test case: IRunnerProxy implemented, executed locally
                ts.VerifyLocalProxy();
                //Test case: IRunnerProxy implemented, executed remotely and ran to completion
                ts.VerifyRemoteProxyCompleted();
                //Test case: IRunnerProxy implemented, executed remotely and cancelled
                ts.VerifyRemoteProxyCanceled();
            }
        }

        [Fact]
        //Test group: test RunExtensions.AbortAsync
        public void AbortAsync()
        {
            using(TestSetupAbort ts = new()) {
                //Test case: IRunnerProxy not implemented
                ts.VerifyNoProxy();
                //Test case: IRunnerProxy implemented, executed locally
                ts.VerifyLocalProxy();
                //Test case: IRunnerProxy implemented, executed remotely and ran to completion
                ts.VerifyRemoteProxyCompleted();
                //Test case: IRunnerProxy implemented, executed remotely and cancelled
                ts.VerifyRemoteProxyCanceled();
            }
        }

        [Fact]
        //Test group: test RunExtensions.GetProgressAsync
        public void GetProgressAsync()
        {
            using(TestSetupNoParams<RunnerBkgProgress> ts = new(new RunnerBkgProgress(1,42)
                , new RunnerBkgProgress(42, null)
                , s => s.GetProgress()
                , s => s.GetProgressAsync(It.IsAny<CancellationToken>()),
                (s, c) => RunnerExtensions.GetProgressAsync(s, c))
            ) {
                //Test case: IRunnerProxy not implemented
                ts.VerifyNoProxy();
                //Test case: IRunnerProxy implemented, executed locally
                ts.VerifyLocalProxy();
                //Test case: IRunnerProxy implemented, executed remotely and ran to completion
                ts.VerifyRemoteProxyCompleted();
                //Test case: IRunnerProxy implemented, executed remotely and cancelled
                ts.VerifyRemoteProxyCanceled();
            }
        }

        [Fact]
        //Test group: test RunExtensions.GetIsBackgroundExecutionCompletedAsync
        public void IsBackgroundExecutionCompletedAsyncTest()
        {
            using(TestSetupNoParams<Boolean> ts = new(false
                , true
                , s => s.IsBackgroundExecutionCompleted
                , s => s.GetIsBackgroundExecutionCompletedAsync(It.IsAny<CancellationToken>()),
                (s, c) => RunnerExtensions.GetIsBackgroundExecutionCompletedAsync(s, c))
            ) {
                //Test case: IRunnerProxy not implemented
                ts.VerifyNoProxy();
                //Test case: IRunnerProxy implemented, executed locally
                ts.VerifyLocalProxy();
                //Test case: IRunnerProxy implemented, executed remotely and ran to completion
                ts.VerifyRemoteProxyCompleted();
                //Test case: IRunnerProxy implemented, executed remotely and cancelled
                ts.VerifyRemoteProxyCanceled();
            }
        }

        [Fact]
        //Test group: test RunExtensions.GetAvailableAsync
        public void GetAvailableAsyncTest()
        {
            using(TestSetupGetAvailable ts = new(
                new RunnerResult<Int32>(1,RunnerStatus.Stalled, 1),
                new RunnerResult<Int32>(42, RunnerStatus.Completed, 42))) 
            {
                //Test case: IRunnerProxy not implemented
                ts.VerifyNoProxy();
                //Test case: IRunnerProxy implemented, executed locally
                ts.VerifyLocalProxy();
                //Test case: IRunnerProxy implemented, executed remotely and ran to completion
                ts.VerifyRemoteProxyCompleted();
                //Test case: IRunnerProxy implemented, executed remotely and cancelled
                ts.VerifyRemoteProxyCanceled();
            }
        }

        //Test group: test RunnerExtensions.GetRequired
        [Fact]
        public void GetRequired()
        {
            Mock<IRunner<Int32>> stubRunner;
            Task<RunnerResult<Int32>> task;
            using(ManualResetEventSlim pause_event=new ManualResetEventSlim()) {
                //Test case: GetRequiredAsync executed synchronously
                stubRunner=new Mock<IRunner<Int32>>();
                stubRunner.Setup(s => s.GetRequiredAsync(It.IsAny<Int32>(), It.IsAny<CancellationToken>(), It.IsAny<Int32>(), It.IsAny<String?>()))
                    .Returns(new ValueTask<RunnerResult<Int32>>(default(RunnerResult<Int32>)))
                ;
                Assert.Equal(default, RunnerExtensions.GetRequired(stubRunner.Object));
                //Test case: GetRequiredAsync executed asynchronously
                stubRunner=new Mock<IRunner<Int32>>();
                stubRunner.Setup(s => s.GetRequiredAsync(It.IsAny<Int32>(), It.IsAny<CancellationToken>(), It.IsAny<Int32>(), It.IsAny<String?>()))
                    .Returns(new ValueTask<RunnerResult<Int32>>(
                        Task.Run(() => { pause_event.Wait(); return default(RunnerResult<Int32>); })))
                ;
                pause_event.Reset();
                task=Task.Run(() => RunnerExtensions.GetRequired(stubRunner.Object));
                Thread.Sleep(50);
                Assert.False(task.IsCompleted);
                pause_event.Set();
                Assert.True(task.Wait(TIMEOUT));
                Assert.Equal(default, task.Result);
                using(CancellationTokenSource cts = new CancellationTokenSource()) {
                    //Test case: GetRequiredAsync canceled
                    stubRunner=new Mock<IRunner<Int32>>();
                    stubRunner.Setup(s => s.GetRequiredAsync(It.IsAny<Int32>(), It.IsAny<CancellationToken>(), It.IsAny<Int32>(), It.IsAny<String?>()))
                        .Returns(new ValueTask<RunnerResult<Int32>>(
                            Task.Run(() => { pause_event.Wait(cts.Token); return default(RunnerResult<Int32>); })))
                    ;
                    pause_event.Reset();
                    task=Task.Run(() => RunnerExtensions.GetRequired(stubRunner.Object));
                    Thread.Sleep(50);
                    Assert.False(task.IsCompleted);
                    cts.Cancel();
                    AggregateException exception = Assert.Throws<AggregateException>(() => task.Wait(TIMEOUT)); 
                    Assert.Single(exception.InnerExceptions);
                    Assert.IsType<OperationCanceledException>(exception.InnerExceptions[0]);
                }
            }
        }

        class TestSetupBase
        {
            Boolean _Local;
            protected Mock<IRunner<Int32>>? _stubRunner;
            protected Mock<IRunnerProxy<Int32>>? _stubProxy;

            protected virtual void SetupNewRunner(Boolean ImplementProxy = false, Boolean LocalProxy = false)
            {
                _stubRunner = new Mock<IRunner<Int32>>();
                _Local=LocalProxy;
                if(ImplementProxy) {
                    _stubProxy = _stubRunner.As<IRunnerProxy<Int32>>();
                    _stubProxy.SetupGet(s => s.IsLocal).Returns(() => _Local);
                }
                else _Local=true;
            }

        }

        class TestSetupIsLocal : TestSetupBase
        {
            public void VerifyNoProxy()
            {
                SetupNewRunner();
                Assert.True(_stubRunner!.Object.IsLocal());
            }

            public void VerifyLocalProxy()
            {
                SetupNewRunner(true,true);
                Assert.True(_stubRunner!.Object.IsLocal());
            }

            public void VerifyRemoteProxy()
            {
                SetupNewRunner(true);
                Assert.False(_stubRunner!.Object.IsLocal());
            }

        }

        class TestSetupAbort: TestSetupBase, IDisposable
        {
            readonly ManualResetEventSlim _pauseEvent;
            readonly Expression<Action<IRunner<Int32>>> _syncExpr;
            readonly Expression<Func<IRunnerProxy<Int32>, Task>> _asyncExpr;


            public TestSetupAbort()
            {
                _pauseEvent = new ManualResetEventSlim(false);
                _syncExpr = s => s.Abort(It.IsAny<String?>());
                _asyncExpr = s => s.AbortAsync(It.IsAny<String?>(), It.IsAny<CancellationToken>());
            }

            public void Dispose()
            {
                _pauseEvent.Dispose();
            }

            protected override void SetupNewRunner(Boolean ImplementProxy = false, Boolean LocalProxy = false)
            {
                base.SetupNewRunner(ImplementProxy, LocalProxy);
                SetupSyncCall();
                if(ImplementProxy) SetupAsyncCall();
            }

            Task GetResultTask(CancellationToken Token)
            {
                return RunnerExtensions.AbortAsync(_stubRunner!.Object, Token: Token);
            }

            void SetupAsyncCall()
            {
                _stubProxy!.Setup(_asyncExpr)
                    .Returns((String? _, CancellationToken token) =>
                    {
                        return Task.Run(
                            () => _pauseEvent.Wait(token), token);
                    });
            }

            void SetupSyncCall()
            {
                _stubRunner!.Setup(_syncExpr);
            }

            void VerifySyncCall(Func<Times> Times)
            {
                _stubRunner!.Verify(_syncExpr, Times);
            }

            void VerifyAsyncCall(Func<Times> Times)
            {
                _stubProxy!.Verify(_asyncExpr, Times);
            }

            public void VerifyNoProxy()
            {
                Task result_task;
                SetupNewRunner(false);
                result_task = GetResultTask(default);
                Assert.True(result_task.IsCompleted);
                VerifySyncCall(Times.Once);
            }

            public void VerifyLocalProxy()
            {
                Task result_task;
                SetupNewRunner(true, true);
                result_task = GetResultTask(default);
                Assert.True(result_task.IsCompleted);
                VerifySyncCall(Times.Once);
                VerifyAsyncCall(Times.Never);
            }

            public void VerifyRemoteProxyCompleted()
            {
                Task result_task;
                _pauseEvent.Reset();
                SetupNewRunner(true);
                result_task = GetResultTask(default);
                Assert.False(result_task.IsCompleted);
                _pauseEvent.Set();
                Assert.True(result_task.Wait(TIMEOUT));
                VerifySyncCall(Times.Never);
                VerifyAsyncCall(Times.Once);
            }

            public void VerifyRemoteProxyCanceled()
            {
                Task result_task;
                using(CancellationTokenSource cts = new CancellationTokenSource()) {
                    _pauseEvent.Reset();
                    SetupNewRunner(true);
                    result_task = GetResultTask(cts.Token);
                    Assert.False(result_task.IsCompleted);
                    cts.Cancel();
                    AggregateException exception = Assert.Throws<AggregateException>(() => result_task.Wait(TIMEOUT));
                    Assert.Single(exception.InnerExceptions);
                    Assert.IsType<TaskCanceledException>(exception.InnerExceptions[0]);
                    VerifySyncCall(Times.Never);
                    VerifyAsyncCall(Times.Once);
                }
            }
        }

        abstract class TestSetupValueTask<TResult> : TestSetupBase, IDisposable
        {
            protected readonly TResult _syncResult, _asyncResult;
            protected readonly ManualResetEventSlim _pauseEvent;

            public TestSetupValueTask(TResult SyncResult, TResult AsyncResult)
            {
                _syncResult=SyncResult;
                _asyncResult=AsyncResult;
                _pauseEvent = new ManualResetEventSlim(false);
            }

            public void Dispose()
            {
                _pauseEvent.Dispose();
            }

            protected override void SetupNewRunner(Boolean ImplementProxy = false, Boolean LocalProxy = false)
            {
                base.SetupNewRunner(ImplementProxy,LocalProxy);
                SetupSyncCall();
                if(ImplementProxy) SetupAsyncCall();
            }

            protected abstract ValueTask<TResult> GetResultTask(CancellationToken Token);
            protected abstract void SetupSyncCall();
            protected abstract void SetupAsyncCall();
            protected abstract void VerifySyncCall(Func<Times> Times);
            protected abstract void VerifyAsyncCall(Func<Times> Times);

            public void VerifyNoProxy()
            {
                ValueTask<TResult> result_value_task;
                SetupNewRunner(false);
                result_value_task = GetResultTask(default);
                Assert.True(result_value_task.IsCompleted);
                Assert.Equal(_syncResult, result_value_task.GetAwaiter().GetResult());
                VerifySyncCall(Times.Once);
            }

            public void VerifyLocalProxy()
            {
                ValueTask<TResult> result_value_task;
                SetupNewRunner(true, true);
                result_value_task = GetResultTask(default);
                Assert.True(result_value_task.IsCompleted);
                Assert.Equal(_syncResult, result_value_task.GetAwaiter().GetResult());
                VerifySyncCall(Times.Once);
                VerifyAsyncCall(Times.Never);
            }

            public void VerifyRemoteProxyCompleted()
            {
                ValueTask<TResult> result_value_task;
                Task<TResult>? result_task;
                _pauseEvent.Reset();
                SetupNewRunner(true);
                result_value_task = GetResultTask(default);
                Assert.False(result_value_task.IsCompleted);
                result_task=result_value_task.AsTask();
                _pauseEvent.Set();
                Assert.True(result_task.Wait(TIMEOUT));
                Assert.Equal(_asyncResult, result_task.GetAwaiter().GetResult());
                VerifySyncCall(Times.Never);
                VerifyAsyncCall(Times.Once);
            }

            public void VerifyRemoteProxyCanceled()
            {
                ValueTask<TResult> result_value_task;
                Task<TResult>? result_task;
                using(CancellationTokenSource cts = new CancellationTokenSource()) {
                    _pauseEvent.Reset();
                    SetupNewRunner(true);
                    result_value_task = GetResultTask(cts.Token);
                    Assert.False(result_value_task.IsCompleted);
                    result_task=result_value_task.AsTask();
                    cts.Cancel();
                    AggregateException exception = Assert.Throws<AggregateException>(() => result_task.Wait(TIMEOUT));
                    Assert.Single(exception.InnerExceptions);
                    Assert.IsType<TaskCanceledException>(exception.InnerExceptions[0]);
                    VerifySyncCall(Times.Never);
                    VerifyAsyncCall(Times.Once);
                }
            }
        }

        class TestSetupNoParams<TResult> : TestSetupValueTask<TResult>
        {
            readonly Expression<Func<IRunner<Int32>, CancellationToken, ValueTask<TResult>>> _runExpression;
            readonly Expression<Func<IRunner<Int32>, TResult>> _syncExpr;
            readonly Expression<Func<IRunnerProxy<Int32>, ValueTask<TResult>>> _asyncExpr;

            public TestSetupNoParams(TResult SyncResult, TResult AsyncResult,
                Expression<Func<IRunner<Int32>, TResult>> PropertyExpr,
                Expression<Func<IRunnerProxy<Int32>, ValueTask<TResult>>> AsyncPropertyExpr,
                Expression<Func<IRunner<Int32>, CancellationToken, ValueTask<TResult>>> RunExpression
                ) : base(SyncResult,AsyncResult)
            {
                _runExpression=RunExpression;
                _syncExpr=PropertyExpr;
                _asyncExpr=AsyncPropertyExpr;

            }

            protected override ValueTask<TResult> GetResultTask(CancellationToken Token)
            {
                Func<IRunner<Int32>, CancellationToken, ValueTask<TResult>> result_delegate = _runExpression.Compile();
                return result_delegate(_stubRunner!.Object, Token);
            }

            protected override void SetupSyncCall()
            {
                _stubRunner!.Setup(_syncExpr).Returns(() => _syncResult);
            }

            protected override void SetupAsyncCall()
            {
                _stubProxy!.Setup(_asyncExpr)
                    .Returns((CancellationToken token) =>
                    {
                        return new ValueTask<TResult>(Task<TResult>.Run(
                            () => { _pauseEvent.Wait(token); return _asyncResult; }, token));
                    });
            }

            protected override void VerifySyncCall(Func<Times> Times)
            {
                _stubRunner!.Verify(_syncExpr, Times);
            }
            
            protected override void VerifyAsyncCall(Func<Times> Times)
            {
                _stubProxy!.Verify(_asyncExpr, Times);
            }

        }

        class TestSetupGetAvailable : TestSetupValueTask<RunnerResult<Int32>>
        {
            readonly Expression<Func<IRunner<Int32>, RunnerResult<Int32>>> _syncExpr;
            readonly Expression<Func<IRunnerProxy<Int32>, ValueTask<RunnerResult<Int32>>>> _asyncExpr;


            public TestSetupGetAvailable(RunnerResult<Int32> SyncResult, RunnerResult<Int32> AsyncResult) 
                : base(SyncResult, AsyncResult)
            {
                _syncExpr = s=>s.GetAvailable(It.IsAny<Int32>(), It.IsAny<Int32>(), It.IsAny<String?>());
                _asyncExpr = s=>s.GetAvailableAsync(It.IsAny<Int32>(), It.IsAny<Int32>(), It.IsAny<String?>(), It.IsAny<CancellationToken>());
            }

            protected override ValueTask<RunnerResult<Int32>> GetResultTask(CancellationToken Token)
            {
                return RunnerExtensions.GetAvailableAsync<Int32>(_stubRunner!.Object, Token:Token);
            }

            protected override void SetupAsyncCall()
            {
                _stubProxy!.Setup(_asyncExpr)
                    .Returns((Int32 _, Int32 _, String? _, CancellationToken token) =>
                    {
                        return new ValueTask<RunnerResult<Int32>>(Task<RunnerResult<Int32>>.Run(
                            () => { _pauseEvent.Wait(token); return _asyncResult; }, token));
                    });
            }

            protected override void SetupSyncCall()
            {
                _stubRunner!.Setup(_syncExpr).Returns(() => _syncResult);
            }

            protected override void VerifySyncCall(Func<Times> Times)
            {
                _stubRunner!.Verify(_syncExpr, Times);
            }

            protected override void VerifyAsyncCall(Func<Times> Times)
            {
                _stubProxy!.Verify(_asyncExpr, Times);
            }

        }
    }
}
