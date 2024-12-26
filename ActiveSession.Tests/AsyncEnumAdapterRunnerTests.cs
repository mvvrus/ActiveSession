using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession.Internal;
using MVVrus.AspNetCore.ActiveSession.StdRunner;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RunnerCommonBase = MVVrus.AspNetCore.ActiveSession.EnumerableRunnerBase<System.Int32>;

namespace ActiveSession.Tests
{
    public class AsyncEnumAdapterRunnerTests : EnumerableRunnerTestsBase
    {
        internal override TestEnumerableSetupBase CreateTestSetup()
        {
            return new TestAsyncEnumAdapterSetup();
        }

        internal override String GetTestedTypeName()
        {
            return nameof(AsyncEnumAdapterRunner<Int32>);
        }

        //Test group: check background processing w/o any FetchRequiredAsync task awaiting
        [Fact]
        public Task BackgroundEnumeration()
        {
            return BackgroundEnumerationImpl();
        }

        [Fact]
        //Test group: FetchRequiredAsync normal flow
        public Task FetchRequiredAsync_NoCancel()
        {
            return FetchRequiredAsync_NoCancelImpl();
        }

        [Fact]
        //Test group: FetchRequiredAsync cancellation and aborting
        public Task FetchRequiredAsync_Cancellation()
        {
            return FetchRequiredAsync_CancellationImpl();
        }

        [Fact]
        //Test group: Disposing (DisposeAsync and hence Dispose) tests
        public Task Disposing()
        {
            return DisposingImpl();
        }

        [Fact]
        //Test group: passing parameters to a constructor
        public void Constructor_Params()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            ActiveSessionOptionsSnapshot options = new ActiveSessionOptionsSnapshot(new ActiveSessionOptions());
            int end = 18;
            AsyncEnumAdapterRunner<Int32> runner;
            CtorTestClass source;

            //Test case: default parameters
            source = new CtorTestClass(end);
            using(runner = new AsyncEnumAdapterRunner<Int32>(source, default, options, logger_factory_mock.LoggerFactory.CreateLogger<AsyncEnumAdapterRunner<Int32>>())) {
                Assert.Equal(RunnerStatus.NotStarted, runner.Status);
                runner.StartRunning();
                Assert.NotNull(runner.EnumTask);
                Assert.True(runner.EnumTask.Wait(5000));
            }
            Assert.True(source.Disposed);
            //Test case: non-default StartInConstructor and PassOwnership
            source = new CtorTestClass(end);
            AsyncEnumAdapterParams<int> param = new AsyncEnumAdapterParams<int>
            {
                Source = source,
                PassSourceOnership = false,
                StartInConstructor = true
            };
            using(runner = new AsyncEnumAdapterRunner<Int32>(param, default, options, logger_factory_mock.LoggerFactory.CreateLogger<AsyncEnumAdapterRunner<Int32>>())) {
                Assert.NotEqual(RunnerStatus.NotStarted, runner.Status);
                Assert.NotNull(runner.EnumTask);
                Assert.True(runner.EnumTask.Wait(10000));
            }
            Assert.False(source.Disposed);
        }

        [Fact]
        //Functional test: Normal flow
        public Task Func_NormalFlow()
        {
            return Func_NormalFlowImpl();
        }

        [Fact]
        //Functional test: complete enumeration with less data than GetRequiredAsync is awaiing for
        public Task Func_CompleteWithLessData()
        {
            return Func_CompleteWithLessDataImpl();
        }

        [Fact]
        //Functional test:  Background fails scenario using GetAvailable
        protected Task Func_BkgFailGetAvailable()
        {
            return Func_BkgFailGetAvailableImpl();
        }

        [Fact]
        //Functional test:  Background fails scenario using GetAvailable
        protected Task Func_BkgFailGetRequiredSync()
        {
            return Func_BkgFailGetRequiredSyncImpl();
        }

        [Fact]
        //Functional test:  Background fails scenario using GetRequiredAsync in asynchronous mode
        public Task Func_BkgFailGetRequiredAsync()
        {
            return Func_BkgFailGetRequiredAsyncImpl();
        }

        [Fact]
        //Functional test: Abort, no GetRequiredAsync awaiting
        public Task Func_AbortNoAwait()
        {
            return Func_AbortNoAwaitImpl();
        }

        [Fact]
        //Functional test: Abort, GetRequiredAsync awaiting
        public Task Func_AbortAwait()
        {
            return Func_AbortAwaitImpl();
        }

        [Fact]
        //Functional test: Dispose[Async]() while GetRequiredAsync is awaiting
        public Task Func_DisposeAwait()
        {
            return Func_DisposeAwaitImpl();
        }

        ////////////////////////////////////////////////////////////////////////////////

        class TestAsyncEnumAdapterSetup : TestEnumerableSetupBase
        {
            public TestAsyncEnumAdapterSetup() : base(typeof(EnumAdapterRunner<Int32>)) { }

            protected override RunnerCommonBase CreateRunnerImpl()
            {
                return new AsyncEnumAdapterRunner<Int32>(
                    new AsyncEnumAdapterParams<Int32>()
                    {
                        Source=_testSequence.GetAsyncEnumerable(),
                        EnumAheadLimit=EnumAheadLimit,
                        PassSourceOnership=true
                    }, default,
                    new ActiveSessionOptionsSnapshot(new ActiveSessionOptions()),
                    LoggerFactory.CreateLogger<AsyncEnumAdapterRunner<Int32>>()
                );
            }
        }

        class CtorTestClass : IAsyncEnumerable<int>, IDisposable
        {
            Int32 _max;
            public Boolean Disposed { get; private set; } = false;
            public CtorTestClass(Int32 Max) { _max = Max; }

            public IAsyncEnumerator<Int32> GetAsyncEnumerator(CancellationToken Token) { return new CtorTestEnumerator(_max, Token); }

            public void Dispose() { Disposed = true; }
        }

        class CtorTestEnumerator : IAsyncEnumerator<int>
        {
            Int32 _current = -1;
            Int32 _max;
            CancellationToken _token;
            public Boolean Disposed { get; private set; } = false;

            public CtorTestEnumerator(Int32 Max, CancellationToken Token) { _max = Max; _token = Token; }
            public Int32 Current => _current;
            public ValueTask<Boolean> MoveNextAsync() { 
                _token.ThrowIfCancellationRequested();
                return new ValueTask<Boolean>(++_current < _max); 
            } 
            public ValueTask DisposeAsync() { Disposed = true;  return ValueTask.CompletedTask; }
        }
    }

}
