using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession.StdRunner;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class EnumAdapterRunnerTests
    {
        //Test group: check background processing w/o any FetchRequiredAsync task awaiting
        [Fact]
        public void BackgroundEnumeration()
        {
            int step1, end=20;

            //Test case: start background process and perform incomplete enumeration
            using(TestEnumerable test_enumerable = new TestEnumerable()) {
                step1 = 5;
                test_enumerable.AddFirstPause(step1);
                using(TestRunner runner = new TestRunner(test_enumerable, end)) {
                    runner.StartBackgroundExecution();
                    Assert.NotNull(runner.EnumTask);
                    Assert.True(test_enumerable.WaitForPause());
                    Assert.False(runner.EnumTask.IsCompleted);
                    Assert.False(runner.Queue.IsAddingCompleted);
                    runner.Queue.FetchAndCheck(0, step1);
                    //Test case: enumeration to the end
                    test_enumerable.Resume();
                    Assert.True(runner.EnumTask.Wait(5000));
                    Assert.True(runner.EnumTask.IsCompletedSuccessfully);
                    Assert.True(runner.Queue.IsAddingCompleted);
                    Assert.Null(runner.Exception);
                    runner.Queue.FetchAndCheck(step1, end - step1);
                }
                //Test case Abort call while eumerating
                test_enumerable.ReleaseTestEnumerable();
                step1 = 19;
                test_enumerable.AddFirstPause(step1);
                using(TestRunner runner = new TestRunner(test_enumerable, end)) {
                    runner.StartBackgroundExecution();
                    Assert.NotNull(runner.EnumTask);
                    Assert.True(test_enumerable.WaitForPause());
                    Assert.False(runner.EnumTask.IsCompleted);
                    Assert.False(runner.Queue.IsAddingCompleted);
                    runner.Abort();
                    test_enumerable.Resume();
                    Assert.True(runner.EnumTask.Wait(5000));
                    Assert.True(runner.EnumTask.IsCompletedSuccessfully);
                    Assert.True(runner.Queue.IsAddingCompleted);
                    Assert.Null(runner.Exception);
                    runner.Queue.FetchAndCheck(0, step1);
                }
                //Test case: exception while enumerating
                test_enumerable.ReleaseTestEnumerable();
                step1 = 10;
                test_enumerable.AddFirstPause(step1, () => throw new TestException());
                using(TestRunner runner = new TestRunner(test_enumerable, end)) {
                    runner.StartBackgroundExecution();
                    Assert.NotNull(runner.EnumTask);
                    Assert.True(runner.EnumTask.Wait(5000));
                    Assert.True(runner.EnumTask.IsCompletedSuccessfully);
                    Assert.True(runner.Queue.IsAddingCompleted);
                    Assert.NotNull(runner.Exception);
                    Assert.IsType<TestException>(runner.Exception);
                    runner.Queue.FetchAndCheck(0, step1);
                }

            }
        }

        [Fact]
        //Test group: FetchRequiredAsync normal flow
        public void FetchRequiredAsync_NoCancel()
        {
            int step1, step2, advance, end = 28;
            Task fetch_task;
            List<Int32> result;
            CancellationTokenSource? fetch_cts=null;
            TestRunner? runner=null;
            TestEnumerable test_enumerable = new TestEnumerable();
            try {
                fetch_cts = new CancellationTokenSource();
                step1 = 0;
                test_enumerable.AddFirstPause(step1);
                runner = new TestRunner(test_enumerable, end);
                //Test case: await on the empty queue, background fetch is in progress
                runner.StartRunning();
                Assert.NotNull(runner.EnumTask);
                Assert.True(test_enumerable.WaitForPause());
                Assert.False(runner.EnumTask.IsCompleted);  
                advance = 10;
                result = new List<Int32>();
                fetch_task = runner.FetchRequiredAsync(advance, result, fetch_cts.Token);
                Assert.False(fetch_task.IsCompleted);
                Assert.Empty(result);
                //Test case: await on the insufficiently filled queue, background fetch is in progress
                step2 = 5;
                test_enumerable.AddNextPause(step2-step1);
                test_enumerable.Resume();
                Assert.True(test_enumerable.WaitForPause());
                Assert.False(runner.EnumTask.IsCompleted);
                Assert.False(fetch_task.IsCompleted);
                for(int i = 0; i < 1000 && runner.Queue.Count > 0 ; i++) Thread.Sleep(100);
                Assert.Equal(0, runner.Queue.Count);
                Assert.Equal(step2, result.Count);
                CheckRange(result, 0, step2);
                //Test case: await on the more than sufficiently filled queue, background fetch is in progress
                step1 = step2;
                step2 = 15;
                test_enumerable.AddNextPause(step2 - step1);
                test_enumerable.Resume();
                Assert.True(test_enumerable.WaitForPause());
                Assert.False(runner.EnumTask.IsCompleted);
                Assert.True(fetch_task.Wait(5000));
                Assert.True(fetch_task.IsCompletedSuccessfully);
                Assert.Equal(advance, result.Count);
                CheckRange(result, 0, advance);
                Assert.Equal(step2 - advance, runner.Queue.Count);
                //Test case: await on queue to be filled with just the same amount as requested, background fetch is in progress
                result = new List<Int32>();
                runner.FetchAvailable(advance, result);
                fetch_task = runner.FetchRequiredAsync(advance, result, fetch_cts.Token);
                Assert.False(fetch_task.IsCompleted);
                step1 = step2;
                step2 = 20;
                test_enumerable.AddNextPause(step2 - step1);
                test_enumerable.Resume();
                Assert.True(test_enumerable.WaitForPause());
                Assert.False(runner.EnumTask.IsCompleted);
                Assert.True(fetch_task.Wait(5000));
                Assert.True(fetch_task.IsCompletedSuccessfully);
                Assert.Equal(advance, result.Count);
                CheckRange(result, advance, advance);
                Assert.Equal(0, runner.Queue.Count);
                step1 = step2;
                step2 = 25;
                test_enumerable.AddNextPause(step2 - step1);
                test_enumerable.Resume();
                Assert.True(test_enumerable.WaitForPause());
                Assert.Equal(step2 - 2*advance, runner.Queue.Count);
                //Test case: await on the initially insufficiently filled queue, background fetch is in progress
                result = new List<Int32>();
                runner.FetchAvailable(advance,result);
                fetch_task = runner.FetchRequiredAsync(advance, result, fetch_cts.Token);
                Assert.False(fetch_task.IsCompleted);
                for(int i = 0; i < 1000 && runner.Queue.Count > 0; i++) Thread.Sleep(100);
                Assert.Equal(step2-2*advance, result.Count);
                CheckRange(result, 2*advance, step2-2*advance);
                //Test case: await on the insufficiently filled queue, background fetch is complete
                test_enumerable.Resume();
                Assert.True(runner.EnumTask.Wait(5000));
                Assert.True(fetch_task.Wait(5000));
                Assert.True(fetch_task.IsCompletedSuccessfully);
                Assert.Equal(end-2*advance, result.Count);
                CheckRange(result, 2*advance, end-2*advance);
                Assert.Equal(0, runner.Queue.Count);
            }
            finally {
                runner?.Dispose();
                fetch_cts?.Dispose();
                test_enumerable.Dispose();
            }
        }

        [Fact]
        //Test group: FetchRequiredAsync cancellation and aborting
        public void FetchRequiredAsync_Cancellation()
        {
            TestEnumerable test_enumerable;
            int step1, advance, end = 18;
            Task fetch_task=null!;
            List<Int32> result;
            CancellationTokenSource? fetch_cts = null;
            TestRunner runner;

            using(test_enumerable = new TestEnumerable()) {
                //Test case: cancel the awaiting fetch task
                PerformTest(() => fetch_cts!.Cancel(),
                    () =>
                    {
                        AggregateException e = Assert.Throws<AggregateException>(() => fetch_task.Wait(5000));
                        Assert.Single(e.InnerExceptions);
                        Assert.IsType<TaskCanceledException>(e.InnerExceptions[0]);
                        Assert.True(fetch_task.IsCanceled);
                    });

                //Test case: abort the awaiting fetch task
                PerformTest(() => runner.Abort(),
                    () =>
                    {
                        Assert.True(fetch_task.Wait(5000));
                        Assert.True(fetch_task.IsCompletedSuccessfully);
                    });


                //Test case: dispose runner while fetch task is awaiting
                ValueTask vt = ValueTask.CompletedTask;
                PerformTest(() => { vt = runner.DisposeAsync(); },
                    () => { CheckTaskTerminatedByDispose(fetch_task!); vt.AsTask().Wait(5000); });
            }

            void PerformTest(Action Act, Action Assess)
            {
                test_enumerable.ReleaseTestEnumerable();
                fetch_cts = new CancellationTokenSource();
                try {
                    step1 = 5;
                    test_enumerable.AddFirstPause(step1);
                    using(runner = new TestRunner(test_enumerable, end)) {
                        runner.StartRunning();
                        Assert.NotNull(runner.EnumTask);
                        Assert.True(test_enumerable.WaitForPause());
                        advance = 10;
                        result = new List<Int32>();
                        fetch_task = runner.FetchRequiredAsync(advance, result, fetch_cts.Token);
                        Act();
                        Assess();
                    }
                }
                finally {
                    fetch_cts?.Dispose();
                    fetch_cts = null;
                }
            }

        }

        [Fact]
        //Test group: Disposing (DisposeAsync and hence Dispose) tests
        public void Disposing()
        {
            TestEnumerable test_enumerable;
            int step1, advance, end = 18;
            Task fetch_task = null!;
            List<Int32> result;
            CancellationTokenSource? fetch_cts = null;
            TestRunner runner;
            Task? dispose_task= null;


            using(test_enumerable = new TestEnumerable()) {
                fetch_cts = new CancellationTokenSource();
                //Test case: dispose non-started runner
                PerformTest(() => { }, () => { Assert.True(dispose_task!.Wait(5000)); });
                //Test case: dispose runner with only a background processing started and not completed before disposing
                PerformTest(() => { StartBkg(); },
                    () =>
                    {
                        Assert.False(runner!.EnumTask!.IsCompleted);
                        test_enumerable.Resume();
                        Assert.True(dispose_task!.Wait(5000));
                    });
                //Test case: dispose runner with both fetch and background processing started and not completed before disposing
                PerformTest(
                    () =>
                    {
                        advance = 10;
                        StartFetch();
                        Assert.NotNull(fetch_task);
                        for(int i = 0; i < 1000 && runner.Queue.Count > 0; i++) Thread.Sleep(100);
                        Assert.False(runner!.EnumTask!.IsCompleted);
                    },
                    () =>
                    {
                        Assert.NotNull(fetch_task);
                        CheckTaskTerminatedByDispose(fetch_task!);
                        test_enumerable.Resume();
                        Assert.True(dispose_task!.Wait(5000));
                    });
                //Test case: dispose runner with both fetch and background processing started but only fetch completed before disposing
                PerformTest(
                    () =>
                    {
                        advance = 5;
                        StartFetch();
                        Assert.NotNull(fetch_task);
                        Assert.True(fetch_task!.Wait(5000));
                        Assert.True(fetch_task!.IsCompletedSuccessfully);
                    },
                    () =>
                    {
                        Assert.False(runner!.EnumTask!.IsCompleted);
                        test_enumerable.Resume();
                        Assert.True(dispose_task!.Wait(5000));
                    });
                //Test case: dispose runner with both fetch and background processing started and completed before disposing
                PerformTest(
                    () =>
                    {
                        advance = 20;
                        StartFetch();
                        test_enumerable.Resume();
                        Assert.NotNull(fetch_task);
                        Assert.True(fetch_task!.Wait(5000));
                        Assert.True(fetch_task!.IsCompletedSuccessfully);
                        Assert.True(runner!.EnumTask!.IsCompleted);
                    },
                    () =>
                    {
                        Assert.True(dispose_task!.Wait(5000));
                    });


            }
            
            void StartBkg()
            {
                runner.StartRunning();
                Assert.NotNull(runner.EnumTask);
                Assert.True(test_enumerable.WaitForPause());
            }

            void StartFetch()
            {
                StartBkg();
                result = new List<Int32>();
                fetch_task = runner.FetchRequiredAsync(advance, result, fetch_cts?.Token??default);
            }

            void PerformTest(Action Arrnage, Action Assess)
            {
                test_enumerable.ReleaseTestEnumerable();
                fetch_cts = new CancellationTokenSource();
                try {
                    step1 = 5;
                    test_enumerable.AddFirstPause(step1);
                    runner = new TestRunner(test_enumerable, end);
                    Arrnage();
                    dispose_task=runner.DisposeAsync().AsTask();
                    Assess();
                }
                finally {
                    fetch_cts?.Dispose();
                }
            }
        }

        [Fact]
        //Test group: passing parameters to a constructor
        public void Constructor_Params()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock
                .MonitorLoggerCategory(Utilities.MakeClassCategoryName(typeof(EnumAdapterRunner<Int32>)));
            ActiveSessionOptionsSnapshot options = new ActiveSessionOptionsSnapshot(new ActiveSessionOptions());
            int end = 18;
            EnumAdapterRunner<Int32> runner;
            CtorTestClass source;

            //Test case: default parameters
            source = new CtorTestClass(end);
            using(runner = new EnumAdapterRunner<Int32>(source, default, options, logger_factory_mock.LoggerFactory)) {
                Assert.Equal(RunnerStatus.NotStarted, runner.Status);
                runner.StartRunning();
                Assert.NotNull(runner.EnumTask);
                Assert.True(runner.EnumTask.Wait(5000));
                Assert.True(source.Disposed);
            }
            //Test case: non-default StartInConstructor and PassOwnership
            source = new CtorTestClass(end);
            EnumAdapterParams<int> param= new EnumAdapterParams<int> {
                Source=source,
                PassSourceOnership=false,
                StartInConstructor=true
            };
            using(runner = new EnumAdapterRunner<Int32>(param, default, options, logger_factory_mock.LoggerFactory)) {
                Assert.NotEqual(RunnerStatus.NotStarted, runner.Status);
                Assert.NotNull(runner.EnumTask);
                Assert.True(runner.EnumTask.Wait(5000));
                Assert.False(source.Disposed);
            }
        }

        void CheckTaskTerminatedByDispose(Task task)
        {
            AggregateException e = Assert.Throws<AggregateException>(() => task.Wait(5000));
            Assert.Single(e.InnerExceptions);
            ObjectDisposedException ode = Assert.IsType<ObjectDisposedException>(e.InnerExceptions[0]);
            Assert.Equal(nameof(TestRunner), ode.ObjectName);
            Assert.True(task!.IsFaulted);
        }

        Boolean CheckRange(IEnumerable<Int32> Range, Int32 Start, Int32 Length)
        {
            Int32 item_to_compare = Start;
            foreach(Int32 item in Range) {
                if(item_to_compare++ != item) return false;
            }
            return item_to_compare == Start + Length;
        }

        class TestException : Exception {}

        class TestRunner: EnumAdapterRunner<Int32>
        {
            readonly TestEnumerable _source;

            public TestRunner(TestEnumerable Source, Int32 Max) 
                : base(Source.GetTestEnumerable(Max), default, new ActiveSessionOptionsSnapshot(new ActiveSessionOptions()), Source.LoggerFactory) 
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
            CancellationTokenSource? _cts=null;
            readonly Action _defaultPauseAction;
            readonly MockedLoggerFactory _loggerFactoryMock = new MockedLoggerFactory();

            public TestEnumerable() 
            {
                _defaultPauseAction = () => { _proceedEvent.Reset(); _testEvent.Set(); _proceedEvent.Wait(_cts?.Token??default); };
                _loggerFactoryMock.MonitorLoggerCategory(Utilities.MakeClassCategoryName(typeof(EnumAdapterRunner<Int32>)));
            }

            public ILoggerFactory LoggerFactory { get => _loggerFactoryMock.LoggerFactory; }

            public void AddFirstPause(Int32 PauseBefore, Action? PauseAction=null)
            {
                _pauseAction = PauseAction??_defaultPauseAction;
                _pauseBefore = PauseBefore;
            }

            public void AddNextPause(Int32 Step, Action? PauseAction=null)
            {
                if(_pauseBefore < 0) throw new InvalidOperationException();
                if(Step <= 0) throw new InvalidOperationException();
                _pauseAction = PauseAction??_pauseAction;
                _pauseBefore+=Step;
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

            public IEnumerable<Int32> GetTestEnumerable(Int32 Max)
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                if(Interlocked.CompareExchange(ref _cts, cts, null) != null) {
                    cts.Dispose();
                    throw new InvalidOperationException();
                }
                for(Int32 i = 0; i < Max; i++) {
                    if(i == _pauseBefore) _pauseAction?.Invoke();
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
    }

    static class EnumAdapterRunnerTestsUtil
    {
        public static void FetchAndCheck(this IItemsQueueFacade<Int32> Queue, Int32 StartValue, Int32 Count)
        {
            Assert.Equal(Count, Queue.Count);
            for(int i = 0; i < Count; i++) {
                Int32 Item;
                Assert.True(Queue.TryTake(out Item));
                Assert.Equal(StartValue + i, Item);
            }
        }
    }

    class CtorTestClass: IEnumerable<int>, IDisposable
    {
        readonly Int32 _max;
        public Boolean Disposed { get; private set; } = false;
        public CtorTestClass(Int32 Max)  { _max = Max; }

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
