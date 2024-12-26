using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession.Internal;
using MVVrus.AspNetCore.ActiveSession.StdRunner;
using System.Collections;
using RunnerCommonBase = MVVrus.AspNetCore.ActiveSession.EnumerableRunnerBase<System.Int32>;

namespace ActiveSession.Tests
{
    public class EnumAdapterRunnerTests: EnumerableRunnerTestsBase
    {
        internal override TestEnumerableSetupBase CreateTestSetup()
        {
            return new TestEnumAdapterSetup();
        }

        internal override String GetTestedTypeName()
        {
            return nameof(EnumAdapterRunner<Int32>);
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
            EnumAdapterRunner<Int32> runner;
            CtorTestClass source;

            //Test case: default parameters
            source = new CtorTestClass(end);
            using(runner = new EnumAdapterRunner<Int32>(source, default, options, logger_factory_mock.LoggerFactory.CreateLogger<EnumAdapterRunner<Int32>>())) {
                Assert.Equal(RunnerStatus.NotStarted, runner.Status);
                runner.StartRunning();
                Assert.NotNull(runner.EnumTask);
                Assert.True(runner.EnumTask.Wait(WAIT_TIMEOUT));
                Assert.True(source.Disposed);
            }
            //Test case: non-default StartInConstructor and PassOwnership
            source = new CtorTestClass(end);
            EnumAdapterParams<int> param= new EnumAdapterParams<int> {
                Source=source,
                PassSourceOnership=false,
                StartInConstructor=true
            };
            using(runner = new EnumAdapterRunner<Int32>(param, default, options, logger_factory_mock.LoggerFactory.CreateLogger<EnumAdapterRunner<Int32>>())) {
                Assert.NotEqual(RunnerStatus.NotStarted, runner.Status);
                Assert.NotNull(runner.EnumTask);
                Assert.True(runner.EnumTask.Wait(WAIT_TIMEOUT));
                Assert.False(source.Disposed);
            }
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

        class TestEnumAdapterSetup : TestEnumerableSetupBase
        {
            public TestEnumAdapterSetup(): base(typeof (EnumAdapterRunner<Int32>) ) { }

            protected override RunnerCommonBase CreateRunnerImpl()
            {
                return new EnumAdapterRunner<Int32>(
                    new EnumAdapterParams<Int32>() {
                        Source=_testSequence.GetEnumerable(),
                        EnumAheadLimit=EnumAheadLimit,
                        PassSourceOnership=true
                    }, default, 
                    new ActiveSessionOptionsSnapshot(new ActiveSessionOptions()), 
                    LoggerFactory.CreateLogger<EnumAdapterRunner<Int32>>()
                );
            }
        }

        class CtorTestClass : IEnumerable<int>, IDisposable
        {
            readonly Int32 _max;
            public Boolean Disposed { get; private set; } = false;
            public CtorTestClass(Int32 Max) { _max = Max; }

            public IEnumerator<Int32> GetEnumerator() { return new CtorTestEnumerator(_max); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

            public void Dispose() { Disposed = true; }
        }

        class CtorTestEnumerator : IEnumerator<int>
        {
            Int32 _current = -1;
            readonly Int32 _max;

            public CtorTestEnumerator(Int32 Max) { _max = Max; }
            public Int32 Current => _current;
            Object IEnumerator.Current { get => Current; }
            public Boolean MoveNext() { return ++_current < _max; }
            public void Reset() { _current = -1; }
            public void Dispose() { }
        }

    }

}
