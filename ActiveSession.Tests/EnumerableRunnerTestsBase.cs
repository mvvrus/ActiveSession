using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RunnerCommonBase = MVVrus.AspNetCore.ActiveSession.EnumerableRunnerBase<System.Int32>;

namespace ActiveSession.Tests
{
    public abstract class EnumerableRunnerTestsBase
    {
        protected const Int32 WAIT_TIMEOUT = 5000;
        internal abstract TestEnumerableSetupBase CreateTestSetup();
        internal abstract String GetTestedTypeName();
        protected static Boolean CheckRange(IEnumerable<Int32> Range, Int32 Start, Int32 Length)
        {
            Int32 item_to_compare = Start;
            foreach(Int32 item in Range) {
                if(item_to_compare++ != item) return false;
            }
            return item_to_compare == Start + Length;
        }


        protected static async Task CheckTimeoutAsync(Task Task)
        {
            Task<Task> wait_outcome = Task.WhenAny(Task, Task.Delay(WAIT_TIMEOUT));
            Task wait_result = await wait_outcome;
            Assert.True(ReferenceEquals(Task, wait_result),"Timeout occured.");
            await wait_result;
        }

        protected static async Task<T> CheckTimeoutAsync<T>(Task<T> ATask)
        {
#pragma warning disable VSTHRD105 // Avoid method overloads that assume TaskScheduler.Current
            Task<Task<T>> wait_outcome = Task.WhenAny(ATask, Task.Delay(WAIT_TIMEOUT).ContinueWith((_)=>Task<T>.FromResult<T>(default!)).Unwrap());
#pragma warning restore VSTHRD105 // Avoid method overloads that assume TaskScheduler.Current
            Task<T> wait_result = await wait_outcome;
            Assert.True(ReferenceEquals(ATask, wait_result), "Timeout occured.");
            return await wait_result;
        }

        //Test group: check background processing w/o any FetchRequiredAsync task awaiting
        protected async Task BackgroundEnumerationImpl()
        {
            int step1, end = 20;
            RunnerCommonBase runner;
            Task step_task;

            //Test case: start background process and perform incomplete enumeration
            TestEnumerableSetupBase ts = CreateTestSetup();
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
                await CheckTimeoutAsync(step_task);
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
            step_task=ts.ResumeEnumeration(step1, TestSequence.StopAction.Fail);
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

        //Test group: FetchRequiredAsync normal flow
        protected async Task FetchRequiredAsync_NoCancelImpl()
        {
            int step1, step2, advance, end = 28;
            Task fetch_task;
            List<Int32> result;
            RunnerCommonBase? runner = null;
            Task step_task;
            TestEnumerableSetupBase ts = CreateTestSetup();
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
                for(int i = 0; i < 1000 && runner.QueueCount > 0; i++) await Task.Yield();
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
                for(int i = 0; i < 1000 && runner.QueueCount > 0; i++) await Task.Yield();
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

        //Test group: FetchRequiredAsync cancellation and aborting
        protected async Task FetchRequiredAsync_CancellationImpl()
        {
            TestEnumerableSetupBase ts = CreateTestSetup();
            Task step_task;
            Task fetch_task = null!;
            CancellationTokenSource? fetch_cts = null;
            int step1, advance;
            List<Int32> result;
            RunnerCommonBase? runner = null;

            //Test case: pass already canceled token
            await PerformTest(() => { },
                async () =>
                {
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CheckTimeoutAsync(fetch_task));
                    Assert.True(fetch_task.IsCanceled);
                },
                () => new CancellationToken(true));
            //Test case: cancel the awaiting fetch task
            await PerformTest(() => fetch_cts!.Cancel(),
                async () =>
                {
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CheckTimeoutAsync(fetch_task));
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
                    Assert.Equal(GetTestedTypeName(), ode.ObjectName);
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

        //Test group: Disposing (DisposeAsync and hence Dispose) tests
        protected async Task DisposingImpl()
        {
            RunnerCommonBase runner = null!;
            Task fetch_task = null!;
            List<Int32> result;
            int step1 = 0, advance, end = 18;
            CancellationTokenSource? fetch_cts = null;
            Task step_task = Task.FromException(new InvalidOperationException("Using uninitialized task"));
            TestEnumerableSetupBase ts;
            Task dispose_task = Task.CompletedTask;

            fetch_cts = new CancellationTokenSource();

            //Test case: dispose non-started runner
            ts = CreateTestSetup();
            await PerformTest(() => Task.CompletedTask, async () => { await CheckTimeoutAsync(dispose_task); });
            //Test case: dispose runner with only a background processing started and not completed before disposing
            await PerformTest(
                () =>
                {
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
                    Assert.Equal(GetTestedTypeName(), ode.ObjectName);
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
            ts = CreateTestSetup();
            async Task Finalization()
            {
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

        //Some functional tests that perform checks from the base class public interface perspective
        //Functional test scenarios.

        //Functional test: Normal flow
        protected async Task Func_NormalFlowImpl()
        {
            TestEnumerableSetupBase ts = CreateTestSetup();
            Task step_task;
            int step=0, prev_step=0, advance;
            RunnerCommonBase runner = ts.CreateRunner();
            Task<RunnerResult<IEnumerable<Int32>>> result_task;
            try {
                // - GetRequiredAsync(start background, await on empty queue)
                advance=5;
                step_task=ts.ResumeEnumeration(step);
                result_task = runner.GetRequiredAsync(advance).AsTask();
                await CheckTimeoutAsync(step_task);
                Assert.False(result_task.IsCompleted);
                // ...produce precise amount of data to complete await
                prev_step=step;
                step=advance;
                step_task=ts.ResumeEnumeration(step-prev_step);
                await CheckTimeoutAsync(step_task);
                AssertResult( (prev_step, step-prev_step, RunnerStatus.Stalled, step, null), await CheckTimeoutAsync(result_task));
                // ...produce some more data
                prev_step=step;
                step=15;
                step_task=ts.ResumeEnumeration(step-prev_step);
                await CheckTimeoutAsync(step_task);
                // - GetAvailable(part of)
                AssertResult((prev_step, advance, RunnerStatus.Progressed, prev_step+advance, null), runner.GetAvailable(advance));
                prev_step+=advance;
                // - GetAvailable(rest of)
                AssertResult((prev_step, step-prev_step, RunnerStatus.Stalled, step, null), runner.GetAvailable());
                // - GetAvailable(should return empty)
                AssertResult((step, 0, RunnerStatus.Stalled, step, null), runner.GetAvailable());
                // ...produce some more data
                prev_step=step;
                step=25;
                step_task=ts.ResumeEnumeration(step-prev_step);
                await CheckTimeoutAsync(step_task);
                // - GetRequiredAsync(sync, part of)
                result_task = runner.GetRequiredAsync(advance).AsTask();
                AssertResult((prev_step, advance, RunnerStatus.Progressed, prev_step+advance, null), await CheckTimeoutAsync(result_task));
                prev_step+=advance;
                // - GetRequiredAsync(sync, rest of)
                result_task = runner.GetRequiredAsync(advance).AsTask();
                AssertResult((prev_step, step-prev_step, RunnerStatus.Stalled, step, null), await CheckTimeoutAsync(result_task));
                // ...produce more data
                prev_step=step;
                step=27;
                step_task=ts.ResumeEnumeration(step-prev_step);
                await CheckTimeoutAsync(step_task);
                // - GetRequiredAsync(await more data than left)
                Int32 await_start = prev_step;
                result_task = runner.GetRequiredAsync(advance).AsTask();
                Assert.False(result_task.IsCompleted);
                // ...produce same amount of data that is awaited for and complete
                prev_step=step;
                step=30;
                step_task=ts.ResumeEnumeration(step-prev_step,TestSequence.StopAction.Complete);
                AssertResult((await_start, step-await_start, RunnerStatus.Completed, step, null), await CheckTimeoutAsync(result_task));
                // - GetAvailble (check Status)
                AssertResult((step, 0, RunnerStatus.Completed, step, null), runner.GetAvailable());
            }
            finally {
                ts.ReleaseEnumerable();
                runner.Dispose();
            }
        }

        //Functional test: complete enumeration with less data than GetRequiredAsync is awaiing for
        protected async Task Func_CompleteWithLessDataImpl()
        {
            TestEnumerableSetupBase ts = CreateTestSetup();
            Task step_task;
            int step = 0, prev_step = 0, advance;
            RunnerCommonBase runner = ts.CreateRunner();
            Task<RunnerResult<IEnumerable<Int32>>> result_task;
            try {
                // - GetRequiredAsync(start background, await on empty queue)
                advance=5;
                step_task=ts.ResumeEnumeration(step);
                result_task = runner.GetRequiredAsync(advance).AsTask();
                await CheckTimeoutAsync(step_task);
                Assert.False(result_task.IsCompleted);
                // ...produce less data than awaited and complete
                prev_step=step;
                step=advance-2;
                step_task=ts.ResumeEnumeration(step-prev_step,TestSequence.StopAction.Complete);
                await CheckTimeoutAsync(step_task);
                AssertResult((prev_step, step-prev_step, RunnerStatus.Completed, step, null), await CheckTimeoutAsync(result_task));
                // - GetRequiredAsync(return status)
                result_task = runner.GetRequiredAsync(advance).AsTask();
                AssertResult((step, 0, RunnerStatus.Completed, step, null), await CheckTimeoutAsync(result_task));
            }
            finally {
                ts.ReleaseEnumerable();
                runner.Dispose();
            }
        }

        //Functional test:  Background fails scenario using GetAvailable
        protected async Task Func_BkgFailGetAvailableImpl()
        {
            TestEnumerableSetupBase ts = CreateTestSetup();
            Task step_task;
            int step = 0, prev_step = 0, advance;
            RunnerCommonBase runner = ts.CreateRunner();
            Task<RunnerResult<IEnumerable<Int32>>> result_task;
            try {
                // - GetRequiredAsync(start background, await on empty queue)
                advance=5;
                step_task=ts.ResumeEnumeration(step);
                result_task = runner.GetRequiredAsync(advance).AsTask();
                await CheckTimeoutAsync(step_task);
                // ...produce more data than awaited for and fail
                prev_step=step;
                step=advance+13;
                step_task=ts.ResumeEnumeration(step-prev_step, TestSequence.StopAction.Fail);
                await CheckTimeoutAsync(step_task);
                await CheckTimeoutAsync(result_task);
                Assert.NotNull(runner.EnumTask);
                await CheckTimeoutAsync(runner.EnumTask);
                prev_step+=advance;
                // - GetAvailable(part of)
                AssertResult((prev_step, advance, RunnerStatus.Progressed, prev_step+advance, null), runner.GetAvailable(advance));
                prev_step+=advance;
                // - GetAvailable(rest of, check status and exception)
                AssertResult((prev_step, step-prev_step, RunnerStatus.Failed, step, typeof(TestSequence.TestException)), runner.GetAvailable());
                // - GetAvailable(return status)
                AssertResult((step, 0, RunnerStatus.Failed, step, typeof(TestSequence.TestException)), runner.GetAvailable());
            }
            finally {
                ts.ReleaseEnumerable();
                runner.Dispose();
            }
        }

        //Functional test:  Background fails scenario using GetRequiredAsync in synchronous mode
        protected async Task Func_BkgFailGetRequiredSyncImpl()
        {
            TestEnumerableSetupBase ts = CreateTestSetup();
            Task step_task;
            int step = 0, prev_step = 0, advance;
            RunnerCommonBase runner = ts.CreateRunner();
            Task<RunnerResult<IEnumerable<Int32>>> result_task;
            try {
                // - GetRequiredAsync(start background, await on empty queue)
                advance=5;
                step_task=ts.ResumeEnumeration(step);
                result_task = runner.GetRequiredAsync(advance).AsTask();
                await CheckTimeoutAsync(step_task);
                // ...produce more data than awaited for and fail
                prev_step=step;
                step=advance+13;
                step_task=ts.ResumeEnumeration(step-prev_step, TestSequence.StopAction.Fail);
                await CheckTimeoutAsync(step_task);
                await CheckTimeoutAsync(result_task);
                Assert.NotNull(runner.EnumTask);
                await CheckTimeoutAsync(runner.EnumTask);
                prev_step+=advance;
                // - GetRequiredAsync(sync, part of)
                result_task = runner.GetRequiredAsync(advance).AsTask();
                AssertResult((prev_step, advance, RunnerStatus.Progressed, prev_step+advance, null), await CheckTimeoutAsync(result_task));
                prev_step+=advance;
                // - GetRequiredAsync(sync, rest of)
                result_task = runner.GetRequiredAsync().AsTask();
                AssertResult((prev_step, step-prev_step, RunnerStatus.Failed, step, typeof(TestSequence.TestException)), await CheckTimeoutAsync(result_task));
                // - GetRequiredAsync(return status)
                result_task = runner.GetRequiredAsync().AsTask();
                AssertResult((step, 0, RunnerStatus.Failed, step, typeof(TestSequence.TestException)), await CheckTimeoutAsync(result_task));
            }
            finally {
                ts.ReleaseEnumerable();
                runner.Dispose();
            }
        }

        //Functional test:  Background fails scenario using GetRequiredAsync in asynchronous mode
        protected async Task Func_BkgFailGetRequiredAsyncImpl()
        {
            TestEnumerableSetupBase ts = CreateTestSetup();
            Task step_task;
            int step = 0, prev_step = 0, advance;
            RunnerCommonBase runner = ts.CreateRunner();
            Task<RunnerResult<IEnumerable<Int32>>> result_task;
            try {
                // - GetRequiredAsync(start background, await on empty queue)
                advance=5;
                step_task=ts.ResumeEnumeration(step);
                result_task = runner.GetRequiredAsync(advance).AsTask();
                await CheckTimeoutAsync(step_task);
                // ...produce less data than awaited for and fail
                prev_step=step;
                step=advance-2;
                step_task=ts.ResumeEnumeration(step-prev_step, TestSequence.StopAction.Fail);
                await CheckTimeoutAsync(step_task);
                Assert.NotNull(runner.EnumTask);
                await CheckTimeoutAsync(runner.EnumTask);
                AssertResult((prev_step, step-prev_step, RunnerStatus.Failed, step, typeof(TestSequence.TestException)), await CheckTimeoutAsync(result_task));
            }
            finally {
                ts.ReleaseEnumerable();
                runner.Dispose();
            }

        }

        //Functional test: Abort, no GetRequiredAsync awaiting
        protected async Task Func_AbortNoAwaitImpl()
        {
            TestEnumerableSetupBase ts = CreateTestSetup();
            Task step_task;
            int step = 0, prev_step = 0, advance;
            RunnerCommonBase runner = ts.CreateRunner();
            Task<RunnerResult<IEnumerable<Int32>>> result_task;
            try {
                // - GetRequiredAsync(start background, await on empty queue)
                advance=5;
                step_task=ts.ResumeEnumeration(step);
                result_task = runner.GetRequiredAsync(advance).AsTask();
                await CheckTimeoutAsync(step_task);
                // ...produce precise amount of data to complete await
                prev_step=step;
                step=advance;
                step_task=ts.ResumeEnumeration(step-prev_step);
                await CheckTimeoutAsync(result_task);
                // - Abort()
                Assert.Equal(RunnerStatus.Aborted, runner.Abort());
                ts.ReleaseEnumerable();
                Assert.NotNull(runner.EnumTask);
                await CheckTimeoutAsync(runner.EnumTask);
                // - GetAvailable()
                IEnumerable<Int32> result;
                RunnerStatus status;
                (result,status,_,_)= runner.GetAvailable();
                Assert.Equal(RunnerStatus.Aborted,status);
                CheckRange(result, runner.Position, 0);
                // - GetRequiredAsync()
                (result, status, _, _) = await CheckTimeoutAsync(runner.GetRequiredAsync().AsTask());
                Assert.Equal(RunnerStatus.Aborted, status);
                CheckRange(result, runner.Position, 0);
            }
            finally {
                ts.ReleaseEnumerable();
                runner.Dispose();
            }

        }

        //Functional test: Abort, GetRequiredAsync awaiting
        protected async Task Func_AbortAwaitImpl()
        {
            TestEnumerableSetupBase ts = CreateTestSetup();
            Task step_task;
            int step = 0, prev_step = 0, advance;
            RunnerCommonBase runner = ts.CreateRunner();
            Task<RunnerResult<IEnumerable<Int32>>> result_task;
            try {
                // - GetRequiredAsync(start background, await on empty queue)
                advance=5;
                step_task=ts.ResumeEnumeration(step);
                result_task = runner.GetRequiredAsync(advance).AsTask();
                await CheckTimeoutAsync(step_task);
                // ...produce less data than awaited for
                prev_step=step;
                step=advance-1;
                RunnerStatus status;
                step_task=ts.ResumeEnumeration(step-prev_step);
                // - Abort()
                Assert.Equal(RunnerStatus.Aborted, runner.Abort());
                (_, status, _, _) = await CheckTimeoutAsync(result_task);
                Assert.Equal(RunnerStatus.Aborted, status);
                Assert.NotNull(runner.EnumTask);
                await CheckTimeoutAsync(runner.EnumTask);
            }
            finally {
                ts.ReleaseEnumerable();
                runner.Dispose();
            }

        }

        //Functional test: Dispose[Async]() while GetRequiredAsync is awaiting
        protected async Task Func_DisposeAwaitImpl()
        {
            TestEnumerableSetupBase ts = CreateTestSetup();
            Task step_task;
            int step = 0, prev_step = 0, advance;
            RunnerCommonBase runner = ts.CreateRunner();
            Task<RunnerResult<IEnumerable<Int32>>> result_task;
            try {
                // - GetRequiredAsync(start background, await on empty queue)
                advance=5;
                step_task=ts.ResumeEnumeration(step);
                result_task = runner.GetRequiredAsync(advance).AsTask();
                await CheckTimeoutAsync(step_task);
                // ...produce more less than awaited for
                prev_step=step;
                step=advance-1;
                step_task=ts.ResumeEnumeration(step-prev_step);
            }
            catch {
                ts.ReleaseEnumerable();
                runner.Dispose();
                throw;
            }            
            // - DisposeAsync()
            Task disposed_task = runner.DisposeAsync().AsTask();
            try {
                await Assert.ThrowsAsync<ObjectDisposedException>(() => CheckTimeoutAsync(result_task));
            }
            finally {
                ts.ReleaseEnumerable();
            }
            await CheckTimeoutAsync(disposed_task);
        }

        static void AssertResult(
            (Int32 Start, Int32 Length, RunnerStatus Status, Int32 Position, Type? ExceptionType) Expected,
            RunnerResult<IEnumerable<Int32>> Actual)
        {
            (IEnumerable<Int32> result, RunnerStatus status, Int32 position, Exception? exception) =Actual;
            CheckRange(result, Expected.Start, Expected.Length);
            Assert.Equal(Expected.Status, status);
            Assert.Equal(Expected.Position, position);
            if(Expected.ExceptionType is null) Assert.Null(exception);
            else {
                Assert.NotNull(exception);
                Assert.IsAssignableFrom(Expected.ExceptionType, exception);
            }
        }

    }

    static class EnumerableRunnerTestsBaseExtensions
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

}
