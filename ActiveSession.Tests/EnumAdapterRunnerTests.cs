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
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock
                .MonitorLoggerCategory(Utilities.MakeClassCategoryName(typeof(EnumAdapterRunner<Int32>)));
            ManualResetEventSlim test_event =new ManualResetEventSlim(false);
            ManualResetEventSlim proceed_event = new ManualResetEventSlim(false);
            TestEnumerable test_enumerable = new TestEnumerable();
            ActiveSessionOptionsSnapshot options = new ActiveSessionOptionsSnapshot(new ActiveSessionOptions());
            int step1, end=20;

            //Test case: start background process and perform incomplete enumeration
            step1 = 5;
            test_enumerable.AddFirstPause(step1, () => { test_event.Set(); proceed_event.Wait(); });
            IEnumerable<Int32> source = test_enumerable.GetTestEnumerable(end);
            using(EnumAdapterRunner<Int32> runner = new EnumAdapterRunner<Int32>(source, default, options,logger_factory_mock.LoggerFactory)) {
                runner.StartBackgroundProcessing();
                Assert.NotNull(runner.EnumTask);
                Assert.True(test_event.Wait(5000));
                Assert.False(runner.EnumTask.IsCompleted);
                Assert.False(runner.Queue.IsAddingCompleted);
                runner.Queue.FetchAndCheck(0, step1);
                //Test case: enumeration to the end
                proceed_event.Set();
                Assert.True(runner.EnumTask.Wait(5000));
                Assert.True(runner.EnumTask.IsCompletedSuccessfully);
                Assert.True(runner.Queue.IsAddingCompleted);
                Assert.Null(runner.Exception);
                runner.Queue.FetchAndCheck(step1, end - step1);
            }
            //Test case: exception while enumerating
            test_event.Reset();
            proceed_event.Reset();
            step1 = 10;
            test_enumerable.AddFirstPause(step1, () => throw new TestException() );
            using(EnumAdapterRunner<Int32> runner = new EnumAdapterRunner<Int32>(source, default, options, logger_factory_mock.LoggerFactory)) {
                runner.StartBackgroundProcessing();
                Assert.NotNull(runner.EnumTask);
                Assert.True(runner.EnumTask.Wait(5000));
                Assert.True(runner.EnumTask.IsCompletedSuccessfully);
                Assert.True(runner.Queue.IsAddingCompleted);
                Assert.NotNull(runner.Exception);
                Assert.IsType<TestException>(runner.Exception);
                runner.Queue.FetchAndCheck(0, step1);
            }
            //Test case Abort call while eumerating
            test_event.Reset();
            proceed_event.Reset();
            step1 = 19;
            test_enumerable.AddFirstPause(step1, () => { test_event.Set(); proceed_event.Wait(); });
            using(EnumAdapterRunner<Int32> runner = new EnumAdapterRunner<Int32>(source, default, options, logger_factory_mock.LoggerFactory)) {
                runner.StartBackgroundProcessing();
                Assert.NotNull(runner.EnumTask);
                Assert.True(test_event.Wait(5000));
                Assert.False(runner.EnumTask.IsCompleted);
                Assert.False(runner.Queue.IsAddingCompleted);
                runner.Abort();
                proceed_event.Set();
                Assert.True(runner.EnumTask.Wait(5000));
                Assert.True(runner.EnumTask.IsCompletedSuccessfully);
                Assert.True(runner.Queue.IsAddingCompleted);
                Assert.Null(runner.Exception);
                runner.Queue.FetchAndCheck(0, step1);
            }
        }

        [Fact]
        //Test group: FetchRequiredAsync normal flow
        public void FetchRequiredAsync_NoCancel()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock
                .MonitorLoggerCategory(Utilities.MakeClassCategoryName(typeof(EnumAdapterRunner<Int32>)));
            ManualResetEventSlim test_event = new ManualResetEventSlim(false);
            ManualResetEventSlim proceed_event = new ManualResetEventSlim(false);
            TestEnumerable test_enumerable = new TestEnumerable();
            ActiveSessionOptionsSnapshot options = new ActiveSessionOptionsSnapshot(new ActiveSessionOptions());
            int step1, step2, advance, end = 18;
            Task fetch_task;
            List<Int32> result;
            CancellationTokenSource? fetch_cts=null;
            CancellationTokenSource? runner_cts=null;
            EnumAdapterRunner<Int32> runner;

            fetch_cts = new CancellationTokenSource();
            try {
                runner_cts = new CancellationTokenSource();
                step1 = 0;
                test_enumerable.AddFirstPause(step1, () => { proceed_event.Reset(); test_event.Set(); proceed_event.Wait(runner_cts!.Token); });
                IEnumerable<Int32> source = test_enumerable.GetTestEnumerable(end);
                runner = new EnumAdapterRunner<Int32>(source, default, options, logger_factory_mock.LoggerFactory);
                try {
                    //Test case: await on the empty queue, background fetch is in progress
                    runner.StartRunning();
                    Assert.NotNull(runner.EnumTask);
                    Assert.True(test_event.Wait(5000));
                    Assert.False(runner.EnumTask.IsCompleted);
                    advance = 10;
                    result = new List<Int32>();
                    fetch_task = runner.FetchRequiredAsync(advance, result, fetch_cts.Token);
                    Assert.False(fetch_task.IsCompleted);
                    Assert.Empty(result);
                    //Test case: await on the insufficiently filled queue, background fetch is in progress
                    step2 = 5;
                    test_enumerable.AddNextPause(step2-step1);
                    test_event.Reset();
                    proceed_event.Set();
                    Assert.True(test_event.Wait(5000));
                    Assert.False(runner.EnumTask.IsCompleted);
                    proceed_event.Reset();
                    Assert.False(fetch_task.IsCompleted);
                    for(int i = 0; i < 1000 && runner.Queue.Count > 0 ; i++) Thread.Sleep(100);
                    Assert.Empty(runner.Queue);
                    Assert.Equal(step2, result.Count);
                    CheckRange(result, 0, step2);
                    //Test case: await on the more than sufficiently filled queue, background fetch is in progress
                    step1 = step2;
                    step2 = 15;
                    test_enumerable.AddNextPause(step2 - step1);
                    test_event.Reset();
                    proceed_event.Set();
                    Assert.True(test_event.Wait(5000));
                    Assert.False(runner.EnumTask.IsCompleted);
                    proceed_event.Reset();
                    Assert.True(fetch_task.Wait(5000));
                    Assert.True(fetch_task.IsCompletedSuccessfully);
                    Assert.Equal(advance, result.Count);
                    CheckRange(result, 0, advance);
                    Assert.Equal(step2 - advance, runner.Queue.Count);
                    //Test case: await on the initially insufficiently filled queue, background fetch is in progress
                    result = new List<Int32>();
                    runner.FetchAvailable(advance,result);
                    fetch_task = runner.FetchRequiredAsync(advance, result, fetch_cts.Token);
                    Assert.False(fetch_task.IsCompleted);
                    for(int i = 0; i < 1000 && runner.Queue.Count > 0; i++) Thread.Sleep(100);
                    Assert.Equal(step2-advance, result.Count);
                    CheckRange(result, advance, step2-advance);
                    //Test case: await on the insufficiently filled queue, background fetch is complete
                    test_event.Reset();
                    proceed_event.Set();
                    Assert.True(runner.EnumTask.Wait(5000));
                    proceed_event.Reset();
                    Assert.True(fetch_task.Wait(5000));
                    Assert.True(fetch_task.IsCompletedSuccessfully);
                    Assert.Equal(end-advance, result.Count);
                    CheckRange(result, advance, end-advance);
                    Assert.Empty(runner.Queue);
                }
                finally {
                    runner_cts?.Cancel();
                    runner.Dispose();
                }
            }
            finally {
                runner_cts?.Dispose();
                fetch_cts?.Dispose();
            }
        }

        [Fact]
        //Test group: FetchRequiredAsync cancellation and aborting
        public void FetchRequiredAsync_Cancellation()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock
                .MonitorLoggerCategory(Utilities.MakeClassCategoryName(typeof(EnumAdapterRunner<Int32>)));
            ManualResetEventSlim test_event = new ManualResetEventSlim(false);
            ManualResetEventSlim proceed_event = new ManualResetEventSlim(false);
            TestEnumerable test_enumerable = new TestEnumerable();
            ActiveSessionOptionsSnapshot options = new ActiveSessionOptionsSnapshot(new ActiveSessionOptions());
            int step1, advance, end = 18;
            Task fetch_task=null!;
            List<Int32> result;
            CancellationTokenSource? fetch_cts = null;
            CancellationTokenSource? runner_cts = null;
            EnumAdapterRunner<Int32> runner;

            //Test case: cancel the awaiting fetch task
            PerformTest(()=>fetch_cts!.Cancel(), 
                () => {
                    AggregateException e = Assert.Throws<AggregateException>(() => fetch_task.Wait(5000));
                    Assert.Single(e.InnerExceptions);
                    Assert.IsType<TaskCanceledException>(e.InnerExceptions[0]);
                    Assert.True(fetch_task.IsCanceled);
                });

            //Test case: abort the awaiting fetch task
            PerformTest(()=>runner.Abort(),
                () =>{
                    Assert.True(fetch_task.Wait(5000));
                    Assert.True(fetch_task.IsCompletedSuccessfully);
                });


            //Test case: dispose runner while fetch task is awaiting
            ValueTask vt;
            PerformTest(() => { vt = runner.DisposeAsync();  },
                () => { CheckTaskTerminatedByDispose(fetch_task!); });


            void PerformTest(Action Act, Action Assess)
            {
                fetch_cts = new CancellationTokenSource();
                try {
                    runner_cts = new CancellationTokenSource();
                    step1 = 5;
                    test_enumerable.AddFirstPause(step1, () => { proceed_event.Reset(); test_event.Set(); proceed_event.Wait(runner_cts!.Token); });
                    IEnumerable<Int32> source = test_enumerable.GetTestEnumerable(end);
                    runner = new EnumAdapterRunner<Int32>(source, default, options, logger_factory_mock.LoggerFactory);
                    try {
                        test_event.Reset();
                        runner.StartRunning();
                        Assert.NotNull(runner.EnumTask);
                        Assert.True(test_event.Wait(5000));
                        advance = 10;
                        result = new List<Int32>();
                        fetch_task = runner.FetchRequiredAsync(advance, result, fetch_cts.Token);
                        Act();
                        Assess();
                    }
                    finally {
                        runner_cts?.Cancel();
                        runner.Dispose();
                    }

                }
                finally {
                    runner_cts?.Dispose();
                    fetch_cts?.Dispose();
                }

            }

        }

        [Fact]
        //Test group: Disposing (DisposeAsync and hence Dispose) tests
        public void Disposing()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock
                .MonitorLoggerCategory(Utilities.MakeClassCategoryName(typeof(EnumAdapterRunner<Int32>)));
            ManualResetEventSlim test_event = new ManualResetEventSlim(false);
            ManualResetEventSlim proceed_event = new ManualResetEventSlim(false);
            TestEnumerable test_enumerable = new TestEnumerable();
            ActiveSessionOptionsSnapshot options = new ActiveSessionOptionsSnapshot(new ActiveSessionOptions());
            int step1, advance, end = 18;
            Task fetch_task = null!;
            List<Int32> result;
            CancellationTokenSource? fetch_cts = null;
            CancellationTokenSource? runner_cts = null;
            EnumAdapterRunner<Int32> runner;
            Task? dispose_task= null;

            fetch_cts = new CancellationTokenSource();

            //Test case: dispose non-started runner
            PerformTest(() => { }, () => { Assert.True(dispose_task!.Wait(5000)); });
            //Test case: dispose runner with only a background processing started and not completed before disposing
            PerformTest(() => { StartBkg(); }, 
                () => {
                    Assert.False(runner!.EnumTask!.IsCompleted);
                    proceed_event.Set();
                    Assert.True(dispose_task!.Wait(5000));
                });
            //Test case: dispose runner with both fetch and background processing started and not completed before disposing
            PerformTest( 
                () => {
                    advance = 10;
                    StartFetch(); 
                }, 
                () => {
                    Assert.NotNull(fetch_task);
                    CheckTaskTerminatedByDispose(fetch_task!);
                    Assert.False(runner!.EnumTask!.IsCompleted);
                    proceed_event.Set();
                    Assert.True(dispose_task!.Wait(5000));
                });
            //Test case: dispose runner with both fetch and background processing started but only fetch completed before disposing
            PerformTest(
                () => {
                    advance = 5;
                    StartFetch();
                    Assert.NotNull(fetch_task);
                    Assert.True(fetch_task!.Wait(5000));
                    Assert.True(fetch_task!.IsCompletedSuccessfully);
                },
                () => {
                    Assert.False(runner!.EnumTask!.IsCompleted);
                    proceed_event.Set();
                    Assert.True(dispose_task!.Wait(5000));
                });
            //Test case: dispose runner with both fetch and background processing started and completed before disposing
            PerformTest(
                () => {
                    advance = 20;
                    StartFetch();
                    proceed_event.Set();
                    Assert.NotNull(fetch_task);
                    Assert.True(fetch_task!.Wait(5000));
                    Assert.True(fetch_task!.IsCompletedSuccessfully);
                    Assert.True(runner!.EnumTask!.IsCompleted);
                }, 
                () => {
                    Assert.True(dispose_task!.Wait(5000));
                });

            void StartBkg()
            {
                test_event.Reset();
                runner.StartRunning();
                Assert.NotNull(runner.EnumTask);
                Assert.True(test_event.Wait(5000));
            }

            void StartFetch()
            {
                StartBkg();
                result = new List<Int32>();
                fetch_task = runner.FetchRequiredAsync(advance, result, fetch_cts?.Token??default);
            }

            void PerformTest(Action Arrnage, Action Assess)
            {
                fetch_cts = new CancellationTokenSource();
                try {
                    runner_cts = new CancellationTokenSource();
                    step1 = 5;
                    test_enumerable.AddFirstPause(step1, () => { proceed_event.Reset(); test_event.Set(); proceed_event.Wait(runner_cts!.Token); });
                    IEnumerable<Int32> source = test_enumerable.GetTestEnumerable(end);
                    runner = new EnumAdapterRunner<Int32>(source, default, options, logger_factory_mock.LoggerFactory);
                    Arrnage();
                    dispose_task=runner.DisposeAsync().AsTask();
                    Assess();
                }
                finally {
                    runner_cts?.Cancel();
                    runner_cts?.Dispose();
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
            //TODO Test case: non-default StartInConstructor and PassOwnership
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
            Assert.Equal(nameof(EnumAdapterRunner<Int32>), ode.ObjectName);
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

        class TestEnumerable
        {
           
            Action? _pauseAction = null;
            Int32 _pauseBefore = -1;

            public void AddFirstPause(Int32 PauseBefore, Action PauseAction)
            {
                _pauseAction = PauseAction;
                _pauseBefore = PauseBefore;
            }

            public void AddNextPause(Int32 Step, Action? PauseAction=null)
            {
                if(_pauseBefore < 0) throw new InvalidOperationException();
                if(Step <= 0) throw new InvalidOperationException();
                _pauseAction = PauseAction??_pauseAction;
                _pauseBefore+=Step;
            }

            public IEnumerable<Int32> GetTestEnumerable(Int32 Max)
            {
                lock(this) {
                    for(Int32 i = 0; i < Max; i++) {
                        if(i == _pauseBefore) _pauseAction?.Invoke();
                        yield return i;
                    }
                    _pauseAction = null;
                    _pauseBefore = -1;
                }
            }
        }

    }

    static class EnumAdapterRunnerTestsUtil
    {
        public static void FetchAndCheck(this BlockingCollection<Int32> Queue, Int32 StartValue, Int32 Count)
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
        Int32 _max;
        public Boolean Disposed { get; private set; } = false;
        public CtorTestClass(Int32 Max)  { _max = Max; }

        public IEnumerator<Int32> GetEnumerator() { return new CtorTestEnumerator(_max); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public void Dispose() { Disposed = true; }
    }

    class CtorTestEnumerator : IEnumerator<int>
    {
        Int32 _current = -1;
        Int32 _max;

        public CtorTestEnumerator(Int32 Max) { _max = Max; }
        public Int32 Current => _current;
        Object IEnumerator.Current { get => Current; }
        public Boolean MoveNext() { return ++_current < _max; }
        public void Reset() { _current = -1; }
        public void Dispose() { }
    }

}
