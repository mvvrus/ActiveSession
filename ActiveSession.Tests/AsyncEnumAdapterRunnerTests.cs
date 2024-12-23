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

        ////////////////////////////////////////////////////////////////////////////////

        void CheckTaskTerminatedByDispose(Task task)
        {
            AggregateException e = Assert.Throws<AggregateException>(() => task.Wait(5000));
            Assert.Single(e.InnerExceptions);
            ObjectDisposedException ode = Assert.IsType<ObjectDisposedException>(e.InnerExceptions[0]);
            Assert.Equal(nameof(TestRunner), ode.ObjectName);
            Assert.True(task!.IsFaulted);
        }

        class TestException : Exception { }

        class TestRunner : AsyncEnumAdapterRunner<Int32>
        {
            public const String LOGGERNAME = "TestRunner";
            readonly TestEnumerable _source;

            public TestRunner(TestEnumerable Source, Int32 Max, Int32? QLimit=null)
                : base(Source.GetTestEnumerable(Max), true, null, true, null, QLimit, false,
                      default, new ActiveSessionOptionsSnapshot(new ActiveSessionOptions()), Source.LoggerFactory.CreateLogger<AsyncEnumAdapterRunner<Int32>>())
            {
                _source = Source;
            }

            protected override void PreDispose()
            {
                base.PreDispose();
                _source.CancelPause();
            }
        }

        class TestEnumerable: IDisposable
        {

            Action? _pauseAction = null;
            Int32 _pauseBefore = -1;
            readonly ManualResetEventSlim _testEvent = new ManualResetEventSlim(false);
            readonly ManualResetEventSlim _proceedEvent = new ManualResetEventSlim(false);
            CancellationTokenSource? _cts = null;
            readonly Action _defaultPauseAction;
            readonly MockedLoggerFactory _loggerFactoryMock = new MockedLoggerFactory();

            public TestEnumerable()
            {
                _defaultPauseAction = () => { _proceedEvent.Reset(); _testEvent.Set(); _proceedEvent.Wait(_cts?.Token ?? default); };
                _loggerFactoryMock.MonitorLoggerCategory(Utilities.MakeClassCategoryName(typeof(EnumAdapterRunner<Int32>)));
            }

            public ILoggerFactory LoggerFactory { get => _loggerFactoryMock.LoggerFactory; }

            public void AddFirstPause(Int32 PauseBefore, Action? PauseAction = null)
            {
                _pauseAction = PauseAction ?? _defaultPauseAction;
                _pauseBefore = PauseBefore;
            }

            public void AddNextPause(Int32 Step, Action? PauseAction = null)
            {
                if(_pauseBefore < 0) throw new InvalidOperationException();
                if(Step <= 0) throw new InvalidOperationException();
                _pauseAction = PauseAction ?? _pauseAction;
                _pauseBefore += Step;
            }

            public Boolean WaitForPause()
            {
                return _testEvent.Wait(5000);
            }

            public void Resume()
            {
                _testEvent.Reset();
                _proceedEvent.Set();
            }

            public void CancelPause()
            {
                _cts?.Cancel();
            }

            public void ReleaseTestEnumerable()
            {
                CancelPause();
                CancellationTokenSource? cts = Interlocked.Exchange(ref _cts, null);
                cts?.Dispose();
                _pauseAction = null;
                _pauseBefore = -1;
                _testEvent.Reset();
                _proceedEvent.Set();
            }

            public async IAsyncEnumerable<Int32> GetTestEnumerable(Int32 Max, [EnumeratorCancellation]CancellationToken Token=default)
            {
                CancellationTokenSource cts = 
                    Token.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(Token) : new CancellationTokenSource();
                if(Interlocked.CompareExchange(ref _cts, cts, null) != null) {
                    cts.Dispose();
                    throw new InvalidOperationException();
                }
                for(Int32 i = 0; i < Max; i++) {
                    if(i == _pauseBefore) await Task.Run(()=>_pauseAction?.Invoke()).WaitAsync(Token);
                    yield return i;
                }
                _pauseAction = null;
                _pauseBefore = -1;
            }

            public void Dispose()
            {
                ReleaseTestEnumerable();
                _testEvent.Dispose();
                _proceedEvent.Dispose();
            }

        }

        class TestAsyncEnumAdapterSetup : TestEnumerableSetupBase
        {
            public TestAsyncEnumAdapterSetup() : base(typeof(EnumAdapterRunner<Int32>)) { }

            protected override RunnerCommonBase CreateRunnerImpl()
            {
                return new AsyncEnumAdapterRunner<Int32>(
                    new AsyncEnumAdapterParams<Int32>()
                    {
                        Source=_testSequence.GetAsyncEnumerable(),
                        EnumAheadLimit=EnumAheadLimit
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
