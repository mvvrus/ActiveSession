using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession.Internal;
using MVVrus.AspNetCore.ActiveSession.StdRunner;
using System.Collections;
using RunnerCommonBase = MVVrus.AspNetCore.ActiveSession.EnumerableRunnerBase<System.Int32>;

namespace ActiveSession.Tests
{
    public class EnumAdapterRunnerTests
    {
        const Int32 WAIT_TIMEOUT = -1; //TODO 5000;
        //Test group: check background processing w/o any FetchRequiredAsync task awaiting
        [Fact]
        public async Task BackgroundEnumeration()
        {
            int step1, end=20;
            RunnerCommonBase runner;
            Task step_task;

            //Test case: start background process and perform incomplete enumeration
            TestEnumAdapterSetup ts = new TestEnumAdapterSetup();
            step1 = 5;
            step_task=ts.ResumeEnumeration(step1);
            runner = ts.CreateRunner();
            try {
                await runner.StartBackgroundExecutionAsync();
                Assert.NotNull(runner.EnumTask);
                await CheckTimeoutAsync(step_task);
                Assert.False(runner.EnumTask.IsCompleted);
                Assert.False(runner.QueueIsAddingCompleted);
                runner.FetchAndCheck(0, step1);
                //Test case: enumeration to the end
                step_task=ts.ResumeEnumeration(end-step1, TestSequence.StopAction.Complete);
                await CheckTimeoutAsync(runner.EnumTask);
                Assert.True(runner.EnumTask.IsCompletedSuccessfully);
                Assert.True(runner.QueueIsAddingCompleted);
                Assert.Null(runner.Exception);
                runner.FetchAndCheck(step1, end - step1);
            }
            finally {
                ts.ReleaseEnumerable();
                runner.Dispose();
            }

            //Test case Abort call while eumerating
            step1 = 19;
            step_task=ts.ResumeEnumeration(step1);
            runner = ts.CreateRunner();
            try {
                await runner.StartBackgroundExecutionAsync();
                Assert.NotNull(runner.EnumTask);
                await CheckTimeoutAsync(step_task);
                Assert.False(runner.EnumTask.IsCompleted);
                Assert.False(runner.QueueIsAddingCompleted);
                runner.Abort();
                step_task=ts.ResumeEnumeration(end-step1, TestSequence.StopAction.Complete);
                await CheckTimeoutAsync(runner.EnumTask);
                Assert.True(runner.EnumTask.IsCompletedSuccessfully);
                Assert.True(runner.QueueIsAddingCompleted);
                Assert.Null(runner.Exception);
                runner.FetchAndCheck(0, step1);
            }
            finally {
                ts.ReleaseEnumerable();
                runner.Dispose();
            }
            //Test case: exception while enumerating
            step1 = 10;
            step_task=ts.ResumeEnumeration(step1,TestSequence.StopAction.Fail);
            runner = ts.CreateRunner();
            try {
                await runner.StartBackgroundExecutionAsync();
                Assert.NotNull(runner.EnumTask);
                await CheckTimeoutAsync(step_task);
                await CheckTimeoutAsync(runner.EnumTask);
                Assert.True(runner.EnumTask.IsCompletedSuccessfully);
                Assert.True(runner.QueueIsAddingCompleted);
                Assert.NotNull(runner.Exception);
                Assert.IsType<TestSequence.TestException>(runner.Exception);
                runner.FetchAndCheck(0, step1);
            }
            finally {
                ts.ReleaseEnumerable();
                runner.Dispose();
            }
            //Test case Abort call while enumerating with a full queue
            step_task=ts.ResumeEnumeration(end, TestSequence.StopAction.Complete);
            ts.EnumAheadLimit=end/2;
            runner = ts.CreateRunner();
            try {
                await runner.StartBackgroundExecutionAsync();
                Assert.NotNull(runner.EnumTask);
                int i;
                for(i = 0; i<100 && !runner.IsQueueBlocked; i++) { Thread.Sleep(0); } 
                Assert.False(runner.EnumTask.IsCompleted);
                Assert.False(runner.QueueIsAddingCompleted);
                Assert.Equal(end/2, runner.GetProgress().Progress);
                runner.Abort();
                await CheckTimeoutAsync(step_task);
                await CheckTimeoutAsync(runner.EnumTask);
                Assert.True(runner.EnumTask.IsCompletedSuccessfully);
                Assert.True(runner.QueueIsAddingCompleted);
                Assert.Null(runner.Exception);
            }
            finally {
                ts.ReleaseEnumerable();
                runner.Abort();
                runner.Dispose();
            }
        }

        [Fact]
        //Test group: FetchRequiredAsync normal flow
        public async Task FetchRequiredAsync_NoCancel()
        {
            int step1, step2, advance, end = 28;
            Task fetch_task;
            List<Int32> result;
            RunnerCommonBase? runner=null;
            Task step_task;
            TestEnumAdapterSetup ts = new TestEnumAdapterSetup();
            try {
                runner = ts.CreateRunner();
                step1 = 0;
                step_task = ts.ResumeEnumeration(step1);
                await runner.StartRunningAsync();
                Assert.NotNull(runner.EnumTask);
                //Test case: await on the empty queue, background fetch is in progress
                await CheckTimeoutAsync(step_task);
                Assert.False(runner.EnumTask.IsCompleted);
                advance = 10;
                result = new List<Int32>();
                fetch_task = runner.FetchRequiredAsync(advance, result, default, "<unknown>");
                await Task.Yield();
                Assert.False(fetch_task.IsCompleted);
                Assert.Empty(result);
                //Test case: await on the insufficiently filled queue, background fetch is in progress
                step2 = 5;
                step_task = ts.ResumeEnumeration(step2-step1);
                await CheckTimeoutAsync(step_task);
                Assert.False(runner.EnumTask.IsCompleted);
                Assert.False(fetch_task.IsCompleted);
                //for(int i = 0; i < 1000 && runner.QueueCount > 0; i++) await Task.Yield();
                Assert.Equal(0, runner.QueueCount);
                Assert.Equal(step2, result.Count);
                CheckRange(result, 0, step2);
                //Test case: await on the more than sufficiently filled queue, background fetch is in progress
                step1 = step2;
                step2 = 15;
                step_task = ts.ResumeEnumeration(step2-step1);
                await CheckTimeoutAsync(step_task);
                Assert.False(runner.EnumTask.IsCompleted);
                await CheckTimeoutAsync(fetch_task);
                Assert.True(fetch_task.IsCompletedSuccessfully);
                Assert.Equal(advance, result.Count);
                CheckRange(result, 0, advance);
                Assert.Equal(step2 - advance, runner.QueueCount);
                //Test case: await on queue to be filled with just the same amount as requested, background fetch is in progress
                result = new List<Int32>();
                runner.FetchAvailable(advance, result);
                fetch_task = runner.FetchRequiredAsync(advance, result, default, "<unknown>");
                Assert.False(fetch_task.IsCompleted);
                step1 = step2;
                step2 = 20;
                step_task = ts.ResumeEnumeration(step2-step1);
                await CheckTimeoutAsync(step_task);
                Assert.False(runner.EnumTask.IsCompleted);
                await CheckTimeoutAsync(fetch_task);
                Assert.True(fetch_task.IsCompletedSuccessfully);
                Assert.Equal(advance, result.Count);
                CheckRange(result, advance, advance);
                Assert.Equal(0, runner.QueueCount);
                step1 = step2;
                step2 = 25;
                step_task = ts.ResumeEnumeration(step2-step1);
                await CheckTimeoutAsync(step_task);
                Assert.Equal(step2 - 2*advance, runner.QueueCount);
                //Test case: await on the initially insufficiently filled queue, background fetch is in progress
                result = new List<Int32>();
                runner.FetchAvailable(advance, result);
                fetch_task = runner.FetchRequiredAsync(advance, result, default, "<unknown>");
                Assert.False(fetch_task.IsCompleted);
                //for(int i = 0; i < 1000 && runner.QueueCount > 0; i++) await Task.Yield();
                Assert.Equal(step2-2*advance, result.Count);
                CheckRange(result, 2*advance, step2-2*advance);
                //Test case: await on the insufficiently filled queue, background fetch is complete
                step_task = ts.ResumeEnumeration(end-step2, TestSequence.StopAction.Complete);
                await CheckTimeoutAsync(step_task);
                await CheckTimeoutAsync(runner.EnumTask);
                await CheckTimeoutAsync(fetch_task);
                Assert.True(fetch_task.IsCompletedSuccessfully);
                Assert.Equal(end-2*advance, result.Count);
                CheckRange(result, 2*advance, end-2*advance);
                Assert.Equal(0, runner.QueueCount);
            }
            finally {
                ts.ReleaseEnumerable();
                runner?.Dispose();
            }
        }

        [Fact]
        //Test group: FetchRequiredAsync cancellation and aborting
        public async Task FetchRequiredAsync_Cancellation()
        {
            TestEnumAdapterSetup ts = new TestEnumAdapterSetup();
            Task step_task;
            Task fetch_task=null!;
            CancellationTokenSource? fetch_cts = null;
            int step1, advance;
            List<Int32> result;
            RunnerCommonBase? runner = null;

            //Test case: pass already canceled token
            await PerformTest(() => { },
                async () =>
                {
                    await Assert.ThrowsAsync<OperationCanceledException>(()=>CheckTimeoutAsync(fetch_task));
                    Assert.True(fetch_task.IsCanceled);
                },
                () => new CancellationToken(true));
            //Test case: cancel the awaiting fetch task
            await PerformTest(() => fetch_cts!.Cancel(),
                async () =>
                {
                    await Assert.ThrowsAsync<OperationCanceledException>(() => CheckTimeoutAsync(fetch_task));
                    Assert.True(fetch_task.IsCanceled);
                });
            //Test case: abort the awaiting fetch task
            await PerformTest(() => runner!.Abort(),
                async () =>
                {
                    await CheckTimeoutAsync(fetch_task);
                    Assert.True(fetch_task.IsCompletedSuccessfully);
                    Assert.True(runner!.CompletionToken.IsCancellationRequested);
                });
            //Test case: dispose runner while fetch task is awaiting
            Task dispose_task = Task.CompletedTask;
            await PerformTest(() => { dispose_task = runner!.DisposeAsync().AsTask(); ts.ReleaseEnumerable(); },
                async () => {
                    ObjectDisposedException ode = await Assert.ThrowsAsync<ObjectDisposedException>(() => CheckTimeoutAsync(fetch_task));
                    Assert.Equal(nameof(EnumAdapterRunner<Int32>), ode.ObjectName);
                    Assert.True(fetch_task.IsFaulted);
                    await CheckTimeoutAsync(dispose_task);
                });



            async Task PerformTest(Action Act, Func<Task> Assess, Func<CancellationToken>? MakeToken = null)
            {
                fetch_cts = new CancellationTokenSource();
                MakeToken = MakeToken??(() => fetch_cts.Token);
                try {
                    step1 = 5;
                    step_task = ts.ResumeEnumeration(step1);
                    runner = ts.CreateRunner();
                    try {
                        await runner.StartRunningAsync();
                        Assert.NotNull(runner.EnumTask);
                        await CheckTimeoutAsync(step_task);
                        advance = 10;
                        result = new List<Int32>();
                        fetch_task = runner.FetchRequiredAsync(advance, result, MakeToken(), "<unknown>");
                        Act();
                        await Assess();
                    }
                    finally {
                        ts.ReleaseEnumerable();
                        runner?.Dispose();
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
        public async Task Disposing()
        {
            RunnerCommonBase runner =null!;
            Task fetch_task = null!;
            List<Int32> result;
            int step1=0, advance, end = 18;
            CancellationTokenSource? fetch_cts = null;
            Task step_task=Task.FromException(new InvalidOperationException("Using uninitialized task"));
            TestEnumAdapterSetup ts = new TestEnumAdapterSetup();
            Task dispose_task = Task.CompletedTask;

            fetch_cts = new CancellationTokenSource();

            //Test case: dispose non-started runner
            await PerformTest(() => Task.CompletedTask, async () => { await CheckTimeoutAsync(dispose_task); });
            //Test case: dispose runner with only a background processing started and not completed before disposing
            await PerformTest(
                () => {
                    step1 = 5;
                    step_task = ts.ResumeEnumeration(step1);
                    return StartBkg(); 
                }, 
                async () =>
                {
                    Assert.False(runner.EnumTask!.IsCompleted);
                    await Finalization();
                });
            //Test case: dispose runner with both fetch and background processing started and not completed before disposing
            await PerformTest(
                async () =>
                {
                    step1 = 5;
                    step_task = ts.ResumeEnumeration(step1);
                    advance = 10;
                    await StartFetch();
                    Assert.NotNull(fetch_task);
                    for(int i = 0; i < 1000 && runner.QueueCount > 0; i++) Thread.Sleep(0);
                    Assert.Equal(0, runner.QueueCount);
                    Assert.False(runner!.EnumTask!.IsCompleted);
                },
                async () =>
                {
                    Assert.NotNull(fetch_task);
                    ObjectDisposedException ode = await Assert.ThrowsAsync<ObjectDisposedException>(() => CheckTimeoutAsync(fetch_task));
                    Assert.Equal(nameof(EnumAdapterRunner<Int32>), ode.ObjectName);
                    Assert.True(fetch_task.IsFaulted);
                    await Finalization();
                });
            //Test case: dispose runner with both fetch and background processing started but only fetch completed before disposing
            await PerformTest(
                async () =>
                {
                    step1 = 5;
                    step_task = ts.ResumeEnumeration(step1);
                    advance = 5;
                    await StartFetch();
                    Assert.NotNull(fetch_task);
                    await CheckTimeoutAsync(fetch_task);
                    Assert.True(fetch_task!.IsCompletedSuccessfully);
                },
                async () =>
                {
                    Assert.False(runner!.EnumTask!.IsCompleted);
                    await Finalization();
                });
            //Test case: dispose runner with both fetch and background processing started and completed before disposing
            await PerformTest(
                async () =>
                {
                    advance = 20;
                    step1 = 5;
                    step_task = ts.ResumeEnumeration(step1);
                    await StartFetch();
                    Assert.NotNull(fetch_task);
                    step_task=ts.ResumeEnumeration(end-step1, TestSequence.StopAction.Complete);
                    await step_task;
                    await CheckTimeoutAsync(fetch_task);
                    Assert.True(fetch_task!.IsCompletedSuccessfully);
                    await CheckTimeoutAsync(runner!.EnumTask!);
                },
                async () =>
                {
                    await CheckTimeoutAsync(dispose_task);
                });

            //Supplement local functions
            async Task Finalization() {
                step_task=ts.ResumeEnumeration(end-step1, TestSequence.StopAction.Complete);
                await step_task;
                ts.ReleaseEnumerable();
                await CheckTimeoutAsync(dispose_task);
            }

            async Task StartBkg()
            {
                await runner.StartRunningAsync();
                Assert.NotNull(runner.EnumTask);
                await CheckTimeoutAsync(step_task);
            }

            async Task StartFetch()
            {
                await StartBkg();
                result = new List<Int32>();
                fetch_task = runner.FetchRequiredAsync(advance, result, fetch_cts?.Token??default, "<unknown>");
            }

            async Task PerformTest(Func<Task> Arrnage, Func<Task> Assess)
            {
                fetch_cts = new CancellationTokenSource();
                try {
                    runner = ts.CreateRunner();
                    await Arrnage();
                    dispose_task=runner.DisposeAsync().AsTask();
                    Thread.Sleep(0);
                    await Assess();
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

        //Some functional tests that perform checks from the base class public interface perspective
        //Functional test scenarios.
        //1. Normal flow
        // - GetRequiredAsync(start background, await on empty queue)
        // ...acqure precize amount of data to complete await
        // - GetAvailable(part of)
        // - GetAvailable(rest of)
        // - GetAvailable(should return empty)
        // ...acquire more data
        // - GetRequiredAsync(sync, part of)
        // - GetRequiredAsync(sync, rest of)
        // ...acquire more data
        // - GetRequiredAsync(await more data than left)
        // ...acquire more data than the rest of awaited for and complete
        // - GetAvailble (check Status)

        //2. Complete then awaited more data than than returned before completion
        // - GetRequiredAsync(start background, await on empty queue)
        // ...acquire less data than awaited and complete
        // - GetRequiredAsync(return status)

        //3. Background fails, GetAvailable
        // - GetRequiredAsync(start background, await on empty queue)
        // ...acqure more data than awaited for and fail
        // - GetAvailable(part of)
        // - GetAvailable(rest of, check status and exception)
        // - GetAvailable(return status)

        //4. Background fails, GetRequiredAsync(sync)
        // - GetRequiredAsync(start background, await on empty queue)
        // ...acqure precize amount of data to complete await
        // ...acqure some data and fail
        // - GetRequiredAsync(sync, part of)
        // - GetRequiredAsync(sync, rest of)
        // - GetRequiredAsync(return status)

        //5. Background fails, GetRequiredAsync(awaiting)
        // - GetRequiredAsync(start background, await on empty queue)
        // ...acqure less data than awaited for and fail

        //6. Abort, no GetRequiredAsync awaiting
        // - GetRequiredAsync(start background, await on empty queue)
        // ...acqure precize amount of data to complete await
        // - Abort()
        // - GetAvailable()
        // - GetRequiredAsync()

        //7. Abort, GetRequiredAsync awaiting
        // - GetRequiredAsync(start background, await on empty queue)
        // ...acqure more data than awaited for
        // - Abort()

        //8. Dispose() while GetRequiredAsync is awaiting
        // - GetRequiredAsync(start background, await on empty queue)
        // ...acqure more data than awaited for
        // - Dispose()

        //9. DisposeAsync() while GetRequiredAsync is awaiting
        // - GetRequiredAsync(start background, await on empty queue)
        // ...acqure more data than awaited for
        // - DisposeAsync()

        //10. Method calls on disposed runner
        // - Dispose()
        // - GetAvailable()
        // - GetRequiredAsync()
        // - Abort()

        //Dispose_Test(): Dispose while GetRequiredAsync is awaiting
        //Dispose_Async_Test(): Dispose while GetRequiredAsync is awaiting
        //GetRequiredAsync_AbortAsync
        //GetRequiredAsync_CompletedAsync
        //GetRequiredAsync_FailedAsync
        //GetRequiredAsync_InProgresAsync

        Boolean CheckRange(IEnumerable<Int32> Range, Int32 Start, Int32 Length)
        {
            Int32 item_to_compare = Start;
            foreach(Int32 item in Range) {
                if(item_to_compare++ != item) return false;
            }
            return item_to_compare == Start + Length;
        }


        async Task CheckTimeoutAsync(Task Task)
        {
            Task<Task> wait_outcome = Task.WhenAny(Task, Task.Delay(WAIT_TIMEOUT));
            Task wait_result = await wait_outcome;
            Assert.Same(Task, wait_result);
            await wait_result;
        }

        class TestEnumAdapterSetup : TestEnumerableSetupBase
        {
            public TestEnumAdapterSetup(): base(typeof (EnumAdapterRunner<Int32>) ) { }
            public Int32? EnumAheadLimit { get; set; } =null;

            protected override RunnerCommonBase CreateRunnerImpl()
            {
                return new EnumAdapterRunner<Int32>(
                    new EnumAdapterParams<Int32>() {
                        Source=_testSequence.GetEnumerable(),
                        EnumAheadLimit=EnumAheadLimit
                    }, default, 
                    new ActiveSessionOptionsSnapshot(new ActiveSessionOptions()), 
                    LoggerFactory.CreateLogger<EnumAdapterRunner<Int32>>()
                );
            }
        }

    }

    static class RunnerCommonBaseTestsUtil
    {
        public static void FetchAndCheck(this RunnerCommonBase Runner, Int32 StartValue, Int32 Count)
        {
            Assert.Equal(Count, Runner.QueueCount);
            for(int i = 0; i < Count; i++) {
                Int32 Item;
                Assert.True(Runner.QueueTryTake(out Item));
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
