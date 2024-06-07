using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession.StdRunner;
using System.Reflection;

using static System.Reflection.BindingFlags;

namespace ActiveSession.Tests
{
    public class SessionProcessRunnerTests
    {
        const Int32 TIMEOUT = 5000;

        //Test group: GetAvailable, normal flow
        [Fact]
        public void GetAvailable_Normal()
        {
            const Int32 COUNT = 48;
            TestSetup<Int32> test_setup;
            SessionProcessRunner<Int32> runner;
            Int32 result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            using (test_setup = new TestSetup<Int32>(COUNT, pos => (pos+1)*2, new (Int32, String)[] {
                (COUNT/4, "Pause"), (COUNT/2, "Pause"), (COUNT/4+COUNT/2, "Pause")})) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT,runner));
                    //Test case: StartPosition+Advance<_progress, default StartPosition, explicit Advance
                    (result, status, position, exception) = runner.GetAvailable(COUNT/6);
                    Assert.Equal((COUNT/4)*2, result);
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(COUNT/6, position);
                    Assert.Null(exception);
                    Assert.Equal(COUNT/6, runner.Position);
                    Assert.Equal(RunnerStatus.Progressed, runner.Status);
                    //Test case: Advance<0 (throws)
                    Assert.Throws<ArgumentException>(() => runner.GetAvailable(-2));
                    //Test case: StartPosition<Position (throws)
                    Assert.Throws<ArgumentException>(() => runner.GetAvailable(Int32.MaxValue, 0));
                    //Test case: StartPosition+Advance==_progress, default StartPosition, explicit Advance 
                    (result, status, position, exception) = runner.GetAvailable(COUNT / 4- COUNT / 6);
                    Assert.Equal((COUNT/4) * 2, result);
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(COUNT/4, position);
                    Assert.Null(exception);
                    Assert.Equal(COUNT/4, runner.Position);
                    Assert.Equal(RunnerStatus.Stalled, runner.Status);
                    //Test case: StartPosition+Advance>_progress,  default StartPosition, explicit Advance
                    test_setup.Resume();
                    Assert.True(test_setup.Wait(TIMEOUT,runner));
                    (result, status, position, exception) = runner.GetAvailable(COUNT);
                    Assert.Equal((COUNT/2)*2, result);
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(COUNT/2, position);
                    Assert.Null(exception);
                    Assert.Equal(COUNT/2, runner.Position);
                    Assert.Equal(RunnerStatus.Stalled, runner.Status);
                    //Test case: explicit StartPosition==Position, explicit Advance (StartPosition+Advance<_progress)
                    test_setup.Resume();
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    (result, status, position, exception) = runner.GetAvailable(COUNT/6, COUNT/2);
                    Assert.Equal((COUNT/2+COUNT/4) * 2, result);
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(COUNT/2+COUNT/6, position);
                    Assert.Null(exception);
                    Assert.Equal(COUNT/2+COUNT/6, runner.Position);
                    Assert.Equal(RunnerStatus.Progressed, runner.Status);
                    //Test case: explicit StartPosition>Position, explicit Advance (StartPosition+Advance<_progress)
                    (result, status, position, exception) = runner.GetAvailable(1, COUNT/2 + COUNT/6+1);
                    Assert.Equal((COUNT/2 + COUNT/4) * 2, result);
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(COUNT/2 + COUNT/6+2, position);
                    Assert.Null(exception);
                    Assert.Equal(COUNT/2+COUNT/6+2, runner.Position);
                    Assert.Equal(RunnerStatus.Progressed, runner.Status);
                    //Test case: default Advance and StartPosition
                    (result, status, position, exception) = runner.GetAvailable();
                    Assert.Equal((COUNT/2 + COUNT/4) * 2, result);
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(COUNT/2 + COUNT/4, position);
                    Assert.Null(exception);
                    Assert.Equal(COUNT/2+COUNT/4, runner.Position);
                    Assert.Equal(RunnerStatus.Stalled, runner.Status);
                    //Test case: end of background process, StartPosition+Advance<_progress
                    test_setup.Resume();
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    (result, status, position, exception) = runner.GetAvailable(COUNT / 6);
                    Assert.Equal(COUNT*2, result);
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(COUNT/2 + COUNT/4 + COUNT/6, position);
                    Assert.Null(exception);
                    //Test case: end of background process, StartPosition+Advance>_progress
                    (result, status, position, exception) = runner.GetAvailable();
                    Assert.Equal(COUNT*2, result);
                    Assert.Equal(RunnerStatus.Complete, status);
                    Assert.Equal(COUNT, position);
                    Assert.Null(exception);
                    //Test case: call while Status is Complete
                    (result, status, position, exception) = runner.GetAvailable();
                    Assert.Equal(COUNT*2, result);
                    Assert.Equal(RunnerStatus.Complete, status);
                    Assert.Equal(COUNT, position);
                    Assert.Null(exception);
                }
                //Test case: call for the disposed object
                Assert.Throws<ObjectDisposedException>(()=>runner.GetAvailable());
            }
        }

        //Test group: GetAvailable, exception in background
        [Fact]
        public void GetAvailable_BackgroundFailed()
        {
            const Int32 COUNT = 24;
            TestSetup<Int32> test_setup;
            SessionProcessRunner<Int32> runner;
            Int32 result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] {
                (COUNT/2, "Fail")})) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    //Test case: exception in a background process, StartPosition+Advance<_progress
                    (result, status, position, exception) = runner.GetAvailable(COUNT / 6);
                    Assert.Equal(COUNT, result);
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(COUNT/ 6, position);
                    Assert.Null(exception);
                    //Test case: exception in a background process, StartPosition+Advance>_progress
                    (result, status, position, exception) = runner.GetAvailable();
                    Assert.Equal(COUNT, result);
                    Assert.Equal(RunnerStatus.Failed, status);
                    Assert.Equal(COUNT/2, position);
                    Assert.NotNull(exception);
                    Assert.IsType<TestException>(exception);
                    //Test case: call while Status is Failed
                    (result, status, position, exception) = runner.GetAvailable();
                    Assert.Equal(COUNT, result);
                    Assert.Equal(RunnerStatus.Failed, status);
                    Assert.Equal(COUNT/2, position);
                    Assert.NotNull(exception);
                    Assert.IsType<TestException>(exception);
                }
            }
        }

        //Test group: GetRequiredAsync normal flow
        [Fact]
        public void GetRequiredAsync_Normal()
        {
            const Int32 COUNT = 24;
            TestSetup<Int32> test_setup;
            SessionProcessRunner<Int32> runner;
            Int32 result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            Task<RunnerResult<Int32>> task_sync;

            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] {
                (COUNT/2, "Pause"), (COUNT*2/3, "Pause")})) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    //Test case: default StartPosition, explicit Advance, synchronous (StartPosition+Advance<_progress) 
                    task_sync  = runner.GetRequiredAsync(COUNT / 3).AsTask();
                    Assert.True(task_sync.IsCompletedSuccessfully);
                    (result, status, position, exception) = task_sync.Result;
                    Assert.Equal(COUNT, result);
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(COUNT / 3, position);
                    Assert.Null(exception);
                    Assert.Equal(COUNT/3, runner.Position);
                    Assert.Equal(RunnerStatus.Progressed, runner.Status);
                    //Test case: Advance<0 (throws)
                    Assert.Throws<ArgumentException>(() => runner.GetRequiredAsync(-2));
                    //Test case: StartPosition<Position (throws)
                    Assert.Throws<ArgumentException>(() => runner.GetRequiredAsync(StartPosition: 0));
                    //Test case: explicit StartPosition, default Advance, synchronous
                    task_sync = runner.GetRequiredAsync(StartPosition: COUNT/3+1).AsTask();
                    Assert.True(task_sync.IsCompletedSuccessfully);
                    (result, status, position, exception) = task_sync.Result;
                    Assert.Equal(COUNT, result);
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(COUNT/3+1, position);
                    Assert.Null(exception);
                    Assert.Equal(COUNT/3+1, runner.Position);
                    Assert.Equal(RunnerStatus.Progressed, runner.Status);
                    //Test case: explicit StartPosition& Advance, synchronous (StartPosition+Advance==_progress)
                    task_sync = runner.GetRequiredAsync(1, StartPosition: COUNT / 2-1).AsTask();
                    Assert.True(task_sync.IsCompletedSuccessfully);
                    (result, status, position, exception) = task_sync.Result;
                    Assert.Equal(COUNT, result);
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(COUNT/2, position);
                    Assert.Null(exception);
                    Assert.Equal(COUNT/2, runner.Position);
                    Assert.Equal(RunnerStatus.Stalled, runner.Status);
                    //Test case: default StartPosition & Advance, asynchronous (StartPosition+Advance>_progress) 1/2
                    Task<RunnerResult<Int32>> task1_2p1 = runner.GetRequiredAsync().AsTask();
                    Task.Yield().GetAwaiter().GetResult();
                    Assert.False(task1_2p1.IsCompleted);
                    //Test case: default StartPosition explicit Advance, asynchronous 1/2
                    Task<RunnerResult<Int32>> task1_2p2ae = runner.GetRequiredAsync(2).AsTask();
                    Task.Yield().GetAwaiter().GetResult();
                    Assert.False(task1_2p2ae.IsCompleted);
                    //Test case: explicit StartPosition default Advance, asynchronous 1/2
                    Task<RunnerResult<Int32>> task1_2p2ea = runner.GetRequiredAsync(StartPosition: runner.Position+2).AsTask();
                    Task.Yield().GetAwaiter().GetResult();
                    Assert.False(task1_2p2ea.IsCompleted);
                    //Test case: explicit StartPosition explicit Advance, asynchronous 1/2
                    Task<RunnerResult<Int32>> task1_2p2ee = runner.GetRequiredAsync(1, default, runner.Position+1).AsTask();
                    Task.Yield().GetAwaiter().GetResult();
                    Assert.False(task1_2p2ee.IsCompleted);
                    //Test case: asynchronous, betwen the second stop and the end 1/3
                    Task<RunnerResult<Int32>> task3_4 = runner.GetRequiredAsync(COUNT/4, StartPosition: COUNT/2).AsTask();
                    Task.Yield().GetAwaiter().GetResult();
                    Assert.False(task3_4.IsCompleted);

                    //Advance to the second stop
                    test_setup.Resume();
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    //Test case: default StartPosition & Advance, asynchronous (StartPosition+Advance>_progress) 2/2
                    Assert.True(task1_2p1.Wait(TIMEOUT));
                    Assert.True(task1_2p1.IsCompletedSuccessfully);
                    (result, status, position, exception) = task1_2p1.Result;
                    Assert.Equal((COUNT/2+1)*2, result);
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(RunnerStatus.Progressed, runner.Status);
                    Assert.Equal(COUNT/2+1, position);
                    Assert.Null(exception);
                    //Test case: default StartPosition explicit Advance, asynchronous 2/2
                    Assert.True(task1_2p2ae.Wait(TIMEOUT));
                    Assert.True(task1_2p2ae.IsCompletedSuccessfully);
                    (result, status, position, exception) = task1_2p2ae.Result;
                    Assert.Equal((COUNT/2+2)*2, result);
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(RunnerStatus.Progressed, runner.Status);
                    Assert.Equal(COUNT/2+2, position);
                    Assert.Null(exception);
                    //Test case: explicit StartPosition default Advance, asynchronous 2/2
                    Assert.True(task1_2p2ea.Wait(TIMEOUT));
                    Assert.True(task1_2p2ea.IsCompletedSuccessfully);
                    (result, status, position, exception) = task1_2p2ea.Result;
                    Assert.Equal((COUNT/2+2)*2, result);
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(RunnerStatus.Progressed, runner.Status);
                    Assert.Equal(COUNT/2+2, position);
                    Assert.Null(exception);
                    //Test case: explicit StartPosition explicit Advance, asynchronous 2/2
                    Assert.True(task1_2p2ee.Wait(TIMEOUT));
                    Assert.True(task1_2p2ee.IsCompletedSuccessfully);
                    (result, status, position, exception) = task1_2p2ee.Result;
                    Assert.Equal((COUNT/2+2)*2, result);
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(RunnerStatus.Progressed, runner.Status);
                    Assert.Equal(COUNT/2+2, position);
                    Assert.Equal(COUNT/2+2, runner.Position);
                    Assert.Null(exception);
                    //Test case: default StartPosition & Advance, synchronous
                    Int32 old_pos = runner.Position;
                    task_sync = runner.GetRequiredAsync().AsTask();
                    Assert.True(task_sync.IsCompletedSuccessfully);
                    (result, status, position, exception) = task_sync.Result;
                    Assert.Equal(COUNT*4/3, result);
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(old_pos+1, position);
                    Assert.Null(exception);
                    //Test case: asynchronous, betwen the second stop and the end 2/3
                    Task.Yield().GetAwaiter().GetResult();
                    Assert.False(task3_4.IsCompleted);
                    //Test case: asynchronous, betwen the second stop and the end(2) 1/2
                    Task<RunnerResult<Int32>> task3_4p1 = runner.GetRequiredAsync(1, StartPosition: COUNT*3/4).AsTask();
                    Task.Yield().GetAwaiter().GetResult();
                    Assert.False(task3_4p1.IsCompleted);

                    //Advance to the end
                    test_setup.Resume();
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    //Test case: asynchronous, betwen the second stop and the end 3/3
                    Assert.True(task3_4.Wait(TIMEOUT));
                    Assert.True(task3_4.IsCompletedSuccessfully);
                    (result, status, position, exception) = task3_4.Result;
                    Assert.Equal((COUNT*3/4)*2, result);
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(COUNT*3/4, position);
                    Assert.Null(exception);
                    //Test case: asynchronous, betwen the second stop and the end(2)  2/2
                    Assert.True(task3_4p1.Wait(TIMEOUT));
                    Assert.True(task3_4p1.IsCompletedSuccessfully);
                    (result, status, position, exception) = task3_4p1.Result;
                    Assert.Equal((COUNT*3/4+1)*2, result);
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(COUNT*3/4+1, position);
                    Assert.Null(exception);
                    Assert.Equal(RunnerStatus.Progressed, runner.Status);
                    Assert.Equal(COUNT*3/4+1, runner.Position);
                    //Test case: synchronous, does not reach the end
                    old_pos = runner.Position;
                    task_sync = runner.GetRequiredAsync().AsTask();
                    Assert.True(task_sync.IsCompletedSuccessfully);
                    (result, status, position, exception) = task_sync.Result;
                    Assert.Equal(COUNT*2, result);
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(old_pos+1, position);
                    Assert.Null(exception);
                    Assert.Equal(old_pos+1, runner.Position);
                    //Test case: synchronous, beyond the end
                    task_sync = runner.GetRequiredAsync(Int32.MaxValue).AsTask();
                    Assert.True(task_sync.IsCompletedSuccessfully);
                    (result, status, position, exception) = task_sync.Result;
                    Assert.Equal(COUNT*2, result);
                    Assert.Equal(RunnerStatus.Complete, status);
                    Assert.Equal(COUNT, position);
                    Assert.Null(exception);
                    Assert.Equal(COUNT, runner.Position);
                    //Test case: call while Status is Complete
                    task_sync = runner.GetRequiredAsync(Int32.MaxValue).AsTask();
                    Assert.True(task_sync.IsCompletedSuccessfully);
                    (result, status, position, exception) = task_sync.Result;
                    Assert.Equal(COUNT*2, result);
                    Assert.Equal(RunnerStatus.Complete, status);
                    Assert.Equal(COUNT, position);
                    Assert.Null(exception);
                    Assert.Equal(COUNT, runner.Position);
                }
                //Test case: call for disposed object
                Assert.Throws<ObjectDisposedException>(()=>runner.GetRequiredAsync(Int32.MaxValue).AsTask().Result);
            }
        }

        //Test case: GetRequiredAsync waits while background job completes
        [Fact]
        public void GetRequiredAsync_BeyondCompletion()
        {
            const Int32 COUNT = 24;
            TestSetup<Int32> test_setup;
            SessionProcessRunner<Int32> runner;
            Int32 result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;

            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] {
                (COUNT/2, "Pause")})) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Task<RunnerResult<Int32>> task1 = runner.GetRequiredAsync(COUNT+1).AsTask();
                    Task.Yield().GetAwaiter().GetResult();
                    Assert.False(task1.IsCompleted);
                    Task<RunnerResult<Int32>> task2 = runner.GetRequiredAsync(COUNT*3/2).AsTask();
                    Task.Yield().GetAwaiter().GetResult();
                    Assert.False(task2.IsCompleted);
                    //Run background to the end
                    test_setup.Resume();
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.True(task1.Wait(TIMEOUT));
                    Assert.True(task1.IsCompletedSuccessfully);
                    (result, status, position, exception) = task1.Result;
                    Assert.Equal(COUNT*2, result);
                    Assert.Equal(RunnerStatus.Complete, status);
                    Assert.Equal(COUNT, position);
                    Assert.Null(exception);
                    Assert.Equal(RunnerStatus.Complete, runner.Status);
                    Assert.Equal(COUNT, runner.Position);
                    Assert.True(task2.Wait(TIMEOUT));
                    Assert.True(task2.IsCompletedSuccessfully);
                    (result, status, position, exception) = task2.Result;
                    Assert.Equal(COUNT*2, result);
                    Assert.Equal(RunnerStatus.Complete, status);
                    Assert.Equal(COUNT, position);
                    Assert.Null(exception);
                }
            }
        }

        //Test group: GetRequiredAsync executed synchronously while background job throws an exception
        [Fact]
        public void GetRequiredAsync_BkgFailedSync()
        {
            const Int32 COUNT = 24;
            TestSetup<Int32> test_setup;
            SessionProcessRunner<Int32> runner;
            Int32 result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;

            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] {
                (COUNT*2/3, "Fail")})) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    //Test case: exception in a background process, synchronous, StartPosition+Advance<_progress
                    Task<RunnerResult<Int32>> task_sync = runner.GetRequiredAsync(COUNT/2).AsTask();
                    Assert.True(task_sync.IsCompleted);
                    (result, status, position, exception) = task_sync.Result;
                    Assert.Equal((COUNT*2/3)*2, result);
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(COUNT/2, position);
                    Assert.Null(exception);
                    Assert.Equal(RunnerStatus.Progressed, runner.Status);
                    Assert.Equal(COUNT/2, runner.Position);
                    Assert.Null(runner.Exception);

                    //Test case: exception in a background process, synchronous, StartPosition+Advance>_progress
                    task_sync = runner.GetRequiredAsync(Int32.MaxValue).AsTask();
                    Assert.True(task_sync.IsCompleted);
                    (result, status, position, exception) = task_sync.Result;
                    Assert.Equal((COUNT*2/3)*2, result);
                    Assert.Equal(RunnerStatus.Failed, status);
                    Assert.Equal(COUNT*2/3, position);
                    Assert.NotNull(exception);
                    Assert.IsType<TestException>(exception);
                    Assert.Equal(RunnerStatus.Failed, runner.Status);
                    Assert.Equal(COUNT*2/3, runner.Position);
                    Assert.NotNull(runner.Exception);
                    Assert.IsType<TestException>(runner.Exception);
                    //Test case: call while Status is Failed
                    task_sync = runner.GetRequiredAsync(Int32.MaxValue).AsTask();
                    Assert.True(task_sync.IsCompleted);
                    (result, status, position, exception) = task_sync.Result;
                    Assert.Equal((COUNT*2/3)*2, result);
                    Assert.Equal(RunnerStatus.Failed, status);
                    Assert.Equal(COUNT*2/3, position);
                    Assert.NotNull(exception);
                    Assert.IsType<TestException>(exception);
                    Assert.Equal(RunnerStatus.Failed, runner.Status);
                    Assert.Equal(COUNT*2/3, runner.Position);
                    Assert.NotNull(runner.Exception);
                    Assert.IsType<TestException>(runner.Exception);
                }
            }
        }

        //Test case: GetRequiredAsync awaits while background job throws an exception
        [Fact]
        public void GetRequiredAsync_BkgFailedAsync()
        {
            const Int32 COUNT = 24;
            TestSetup<Int32> test_setup;
            SessionProcessRunner<Int32> runner;
            Int32 result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;

            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] {
                (COUNT/2, "Pause"), (COUNT*2/3, "Fail")})) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Task<RunnerResult<Int32>> task1 = runner.GetRequiredAsync(COUNT+1).AsTask();
                    Task.Yield().GetAwaiter().GetResult();
                    Assert.False(task1.IsCompleted);
                    Task<RunnerResult<Int32>> task2 = runner.GetRequiredAsync(COUNT*3/2).AsTask();
                    Task.Yield().GetAwaiter().GetResult();
                    Assert.False(task2.IsCompleted);
                    test_setup.Resume();

                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.True(task1.Wait(TIMEOUT));
                    Assert.True(task1.IsCompletedSuccessfully);
                    (result, status, position, exception) = task1.Result;
                    Assert.Equal((COUNT*2/3)*2, result);
                    Assert.Equal(RunnerStatus.Failed, status);
                    Assert.Equal(COUNT*2/3, position);
                    Assert.NotNull(exception);
                    Assert.IsType<TestException>(exception);

                    Assert.Equal(RunnerStatus.Failed, runner.Status);
                    Assert.NotNull(runner.Exception);
                    Assert.IsType<TestException>(runner.Exception);
                    Assert.Equal(COUNT*2/3, runner.Position);

                    Assert.True(task2.Wait(TIMEOUT));
                    Assert.True(task2.IsCompletedSuccessfully);
                    (result, status, position, exception) = task2.Result;
                    Assert.Equal((COUNT*2/3)*2, result);
                    Assert.Equal(RunnerStatus.Failed, status);
                    Assert.Equal(COUNT*2/3, position);
                    Assert.NotNull(exception);
                    Assert.IsType<TestException>(exception);
                }
            }
        }

        //Test group: Disposing the runner
        [Fact]
        public void Dispose()
        {
            const Int32 COUNT = 24;
            TestSetup<Int32> test_setup;
            SessionProcessRunner<Int32> runner;
            //Test case: Background process is completed
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { })) {
                runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>());
                Assert.True(test_setup.Wait(TIMEOUT, runner));
                runner.GetAvailable();
                Assert.Equal(RunnerStatus.Complete, runner.Status);
                Assert.True(Task.Run(() => runner.Dispose()).Wait(TIMEOUT));
                Assert.True(runner.IsBackgroundExecutionCompleted);
            }
            //Test case: Background process is not completed, no pending GetRequiredAsync calls
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] {(COUNT/2, "Pause")})) {
                runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>());
                Assert.True(test_setup.Wait(TIMEOUT, runner));
                runner.GetAvailable();
                Assert.Equal(RunnerStatus.Stalled, runner.Status);
                Assert.True(Task.Run(() => runner.Dispose()).Wait(TIMEOUT));
                Assert.True(runner.IsBackgroundExecutionCompleted);
            }
            //Test case: Background process is not completed, some pending GetRequiredAsync calls
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] {(COUNT/2, "Pause")})) {
                runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>());
                Assert.True(test_setup.Wait(TIMEOUT, runner));

                Task<RunnerResult<Int32>> task1 = runner.GetRequiredAsync(COUNT*3/2).AsTask();
                Task.Yield().GetAwaiter().GetResult();
                Assert.False(task1.IsCompleted);
                Task<RunnerResult<Int32>> task2 = runner.GetRequiredAsync(COUNT*2).AsTask();
                Task.Yield().GetAwaiter().GetResult();
                Assert.False(task2.IsCompleted);
                Assert.Equal(RunnerStatus.Progressed, runner.Status);
                Assert.True(Task.Run(() => runner.Dispose()).Wait(TIMEOUT));
                Assert.True(runner.IsBackgroundExecutionCompleted);
                Assert.True(task1.IsFaulted);
                Assert.True(task1.Exception is AggregateException agg1 && agg1.InnerExceptions.Count==1 && agg1.InnerExceptions[0] is ObjectDisposedException);
                Assert.True(task2.IsFaulted);
                Assert.True(task2.Exception is AggregateException agg2 && agg2.InnerExceptions.Count==1 && agg2.InnerExceptions[0] is ObjectDisposedException);
            }
        }

        //Test group: Abort the runner
        [Fact]
        public void Abort()
        {
            const Int32 COUNT = 24;
            TestSetup<Int32> test_setup;
            SessionProcessRunner<Int32> runner;
            //Test case: Background completed, Status in not Completed yet
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { })) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    runner.Abort();
                    Assert.Equal(RunnerStatus.Aborted, runner.Status);
                    //Test case: Runner already aborted
                    runner.Abort();
                    Assert.Equal(RunnerStatus.Aborted, runner.Status);
                }
            }
            //Test case: Background completed, Status in Completed
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { })) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    runner.GetAvailable();
                    runner.Abort();
                    Assert.Equal(RunnerStatus.Complete, runner.Status);
                }
            }
            //Test case: Background failed, Status in not Failed yet
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] {(COUNT/2,"Fail")})) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    runner.Abort();
                    Assert.Equal(RunnerStatus.Aborted, runner.Status);
                    Assert.Null(runner.Exception);
                }
            }
            //Test case: Background failed, Status in Failed
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { (COUNT/2, "Fail") })) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    runner.GetAvailable();
                    runner.Abort();
                    Assert.Equal(RunnerStatus.Failed, runner.Status);
                    Assert.NotNull(runner.Exception);
                    Assert.IsType<TestException>(runner.Exception);
                }
            }
            //Test case: Runner disposed
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { })) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                }
                runner.Abort();
                Assert.Equal(RunnerStatus.Aborted, runner.Status);
            }
            //Test case: Background in progress, Abort at callback
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] {(COUNT/2, "Pause")},
                MonitorCompletionToken:false)) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    runner.GetAvailable();
                    runner.Abort();
                    Assert.False(runner.IsBackgroundExecutionCompleted);
                    test_setup.Resume();
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.True(runner.IsBackgroundExecutionCompleted);
                    Assert.Equal(RunnerStatus.Aborted, runner.Status);
                    Assert.True(test_setup.InCallback);
                }
            }
            //Test case: Background in progress, Abort inside the background task
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] {(COUNT/2, "Pause")})) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Int32 result;
                    RunnerStatus status;
                    Int32 position;
                    Exception? exception;
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    //Test case (nested): GetRequiredAsync was awaiting during Abort call part 1/2
                    Task<RunnerResult<Int32>> task1 = runner.GetRequiredAsync(COUNT*2/3).AsTask();
                    Task.Yield().GetAwaiter().GetResult();
                    Assert.False(task1.IsCompleted);
                    //End of the test case part 1/2
                    runner.GetAvailable();
                    Assert.False(runner.IsBackgroundExecutionCompleted);
                    runner.Abort();
                    Assert.True(test_setup.WaitWithResume(TIMEOUT, runner));
                    Assert.True(runner.IsBackgroundExecutionCompleted);
                    Assert.Equal(RunnerStatus.Aborted, runner.Status);
                    Assert.False(test_setup.InCallback);
                    //Test case: GetRequiredAsync was awaiting during Abort call part 2/2
                    Assert.True(task1.Wait(TIMEOUT));
                    Assert.True(task1.IsCompletedSuccessfully);
                    (result, status, position, exception) = task1.Result;
                    Assert.Equal(RunnerStatus.Aborted, status);
                    //Test case: GetAvailable after Abort
                    (result, status, position, exception) = runner.GetAvailable();
                    Assert.Equal(RunnerStatus.Aborted, status);
                    //Test case: GetRequiredAsync after Abort
                    task1 = runner.GetRequiredAsync(COUNT).AsTask();
                    Assert.True(task1.IsCompletedSuccessfully);
                    (result, status, position, exception) = task1.Result;
                    Assert.Equal(RunnerStatus.Aborted, status);
                }
            }
            //Test case: Background in progress, the background task was canceled by external means
            using(CancellationTokenSource cts=new CancellationTokenSource()) {
                using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { (COUNT/2, "Pause") },
                    ExtToken:cts.Token, MonitorCompletionToken:false)) {
                    using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                        Int32 result;
                        RunnerStatus status;
                        Int32 position;
                        Exception? exception;
                        Assert.True(test_setup.Wait(TIMEOUT, runner));
                        runner.GetAvailable();
                        Assert.False(runner.IsBackgroundExecutionCompleted);
                        //Test case (nested): GetRequiredAsync is awaiting during cancellation by external means part 1/2
                        Task<RunnerResult<Int32>> task1 = runner.GetRequiredAsync(COUNT*2/3).AsTask();
                        Task.Yield().GetAwaiter().GetResult();
                        Assert.False(task1.IsCompleted);
                        //End of the test case part 1/2
                        cts.Cancel();
                        Assert.True(test_setup.WaitWithResume(TIMEOUT, runner));
                        Assert.True(runner.IsBackgroundExecutionCompleted);
                        Assert.Equal(RunnerStatus.Aborted, runner.Status);
                        Assert.False(test_setup.InCallback);
                        Assert.True(test_setup.CanceledExternally);
                        //Test case (nested): GetRequiredAsync is awaiting during cancellation by external means part 2/2
                        Assert.True(task1.Wait(TIMEOUT));
                        Assert.True(task1.IsCompletedSuccessfully);
                        (result, status, position, exception) = task1.Result;
                        Assert.Equal(RunnerStatus.Aborted, status);
                    }
                }
            }
        }

        //Test group: IRunnerBackgroundProgress
        [Fact]
        public void BackgroundProgress() 
        {
            Int32 progress;
            Int32? estimated_end;
            const Int32 COUNT = 24;
            TestSetup<Int32> test_setup;
            SessionProcessRunner<Int32> runner;
            //Test case: In progress, function body, report end estimation
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { (COUNT/2, "Pause") })) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.False(runner.IsBackgroundExecutionCompleted);
                    (progress, estimated_end) = runner.GetProgress();
                    Assert.Equal(COUNT/2,progress);
                    Assert.Equal(COUNT, estimated_end);
                    //Test case: completed, function body, report end estimation
                    test_setup.Resume();
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.True(runner.IsBackgroundExecutionCompleted);
                    (progress, estimated_end) = runner.GetProgress();
                    Assert.Equal(COUNT, progress);
                    Assert.Equal(COUNT, estimated_end);
                    Assert.Equal(COUNT*2, runner.GetAvailable().Result);
                }
            }
            //Test case: In progress, procedure body, report end estimation
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { (COUNT/2, "Pause") })) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.VoidBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.False(runner.IsBackgroundExecutionCompleted);
                    (progress, estimated_end) = runner.GetProgress();
                    Assert.Equal(COUNT/2, progress);
                    Assert.Equal(COUNT, estimated_end);
                    //Test case: completed, procedure body, report end estimation
                    test_setup.Resume();
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.True(runner.IsBackgroundExecutionCompleted);
                    (progress, estimated_end) = runner.GetProgress();
                    Assert.Equal(COUNT, progress);
                    Assert.Equal(COUNT, estimated_end);
                    Assert.Equal(COUNT*2-2, runner.GetAvailable().Result);
                }
            }
            //Test case: failed, report end estimation 
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { (COUNT/2, "Fail") })) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.True(runner.IsBackgroundExecutionCompleted);
                    (progress, estimated_end) = runner.GetProgress();
                    Assert.Equal(COUNT/2, progress);
                    Assert.Equal(COUNT/2, estimated_end);
                }
            }
            //Test case: aborted, report end estimation 
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { (COUNT/2, "Pause") })) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.False(runner.IsBackgroundExecutionCompleted);
                    runner.Abort();
                    Assert.True(test_setup.WaitWithResume(TIMEOUT, runner));
                    Assert.True(runner.IsBackgroundExecutionCompleted);
                    (progress, estimated_end) = runner.GetProgress();
                    Assert.Equal(COUNT/2, progress);
                    Assert.Equal(COUNT/2, estimated_end);
                }
            }
            //Test case: In progress, function body, does not report end estimation
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { (COUNT/2, "Pause") }, 
                ReportCount:false)) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.False(runner.IsBackgroundExecutionCompleted);
                    (progress, estimated_end) = runner.GetProgress();
                    Assert.Equal(COUNT/2, progress);
                    Assert.Null(estimated_end);
                    //Test case: completed, function body, does not report end estimation
                    test_setup.Resume();
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.True(runner.IsBackgroundExecutionCompleted);
                    (progress, estimated_end) = runner.GetProgress();
                    Assert.Equal(COUNT, progress);
                    Assert.Equal(COUNT, estimated_end);
                    Assert.Equal(COUNT*2, runner.GetAvailable().Result);
                }
            }
            //Test case: completed, procedure body, does not report end estimation
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { (COUNT/2, "Pause") },
                ReportCount: false)) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.VoidBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.False(runner.IsBackgroundExecutionCompleted);
                    (progress, estimated_end) = runner.GetProgress();
                    Assert.Equal(COUNT/2, progress);
                    Assert.Null(estimated_end);
                    //Test case: completed, procedure body, does not report end estimation
                    test_setup.Resume();
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.True(runner.IsBackgroundExecutionCompleted);
                    (progress, estimated_end) = runner.GetProgress();
                    Assert.Equal(COUNT, progress);
                    Assert.Equal(COUNT, estimated_end);
                    Assert.Equal(COUNT*2-2, runner.GetAvailable().Result);
                }
            }
            //Test case: failed, does not report end estimation 
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { (COUNT/2, "Fail") },
                ReportCount: false)) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.True(runner.IsBackgroundExecutionCompleted);
                    (progress, estimated_end) = runner.GetProgress();
                    Assert.Equal(COUNT/2, progress);
                    Assert.Equal(COUNT/2, estimated_end);
                }
            }
            //Test case: aborted, does not report end estimation 
            using(test_setup = new TestSetup<Int32>(COUNT, pos => (pos + 1) * 2, new (Int32, String)[] { (COUNT/2, "Pause") },
                ReportCount: false)) {
                using(runner = new SessionProcessRunner<Int32>(test_setup.FuncBody, default, test_setup.LoggerFactory.CreateLogger<SessionProcessRunner<Int32>>())) {
                    Assert.True(test_setup.Wait(TIMEOUT, runner));
                    Assert.False(runner.IsBackgroundExecutionCompleted);
                    runner.Abort();
                    Assert.True(test_setup.WaitWithResume(TIMEOUT, runner));
                    Assert.True(runner.IsBackgroundExecutionCompleted);
                    (progress, estimated_end) = runner.GetProgress();
                    Assert.Equal(COUNT/2, progress);
                    Assert.Equal(COUNT/2, estimated_end);
                }
            }
        }

        //Test case: pass null as a task body to one of constructors
        [Fact]
        public void ConstructorsNullArgument()
        {
            Assert.Throws<ArgumentNullException>(() =>new SessionProcessRunner<Int32>(
                (Action<Action<int,int?>,CancellationToken>)(null!),default,null));
            Assert.Throws<ArgumentNullException>(() => new SessionProcessRunner<Int32>(
                (Func<Action<int, int?>, CancellationToken, int>)(null!), default, null));
        }

        class TestSetup<TResult> : IDisposable
        {
            Int32 _count,_position;
            Boolean _reportCount;
            Boolean _monitorCompletionToken;
            ManualResetEventSlim _pauseEvent, _readyEvent;
            Func<Int32, TResult> _resultFunc;
            SortedList<Int32, Action> _checkPoints;
            Boolean _ranToEnd=false;
            CancellationToken _token = default;
            MockedLoggerFactory _logger_factory_mock;
            MockedLogger _logger_mock;
            CancellationToken  _extToken = default;

            public ILoggerFactory LoggerFactory { get { return _logger_factory_mock.LoggerFactory; } }

            public Boolean CanceledExternally { get; private set; }
            public Boolean InCallback { get; private set; }

            public TestSetup(Int32 Count, Func<Int32, TResult> ResultFunc, 
                IEnumerable<ValueTuple<Int32,String>> CheckPoints, 
                Boolean ReportCount=true, Boolean MonitorCompletionToken=true, CancellationToken ExtToken=default) 
            {
                _count = Count-1;
                _resultFunc = ResultFunc;
                _reportCount = ReportCount;
                _monitorCompletionToken=MonitorCompletionToken;
                _extToken=ExtToken;
                _position = 0;
                _pauseEvent = new ManualResetEventSlim();
                _readyEvent = new ManualResetEventSlim(true);
                _checkPoints = new SortedList<Int32, Action>();
                _logger_factory_mock = new MockedLoggerFactory();
                _logger_mock = _logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
                foreach((Int32 pos, String action_name) in CheckPoints) {
                    MethodInfo action_method = GetType()
                        .GetMethod(action_name, Public | NonPublic | Instance, Type.EmptyTypes) 
                            ?? throw new ArgumentException($"Invalid action name:{action_name}");
                    Action action =action_method.CreateDelegate<Action>(this);
                    _checkPoints.Add(pos, action);
                }
            }

            public TResult FuncBody(Action<TResult, Int32?> ProgressCallback, CancellationToken CompletionToken)
            {
                IList<Int32> cp_keys = _checkPoints.Keys;
                Int32 cp_pos, cp_count = cp_keys.Count;
                _pauseEvent.Reset();
                try {
                    for(_position = 0, cp_pos = 0; _position < _count; _position++) {
                        //Thread.Sleep(10);
                        if(cp_pos < cp_count && cp_keys[cp_pos] == _position) {
                            CancellationTokenSource? compound_source = null;
                            if(_extToken.CanBeCanceled && CompletionToken.CanBeCanceled && _monitorCompletionToken) {
                                compound_source = CancellationTokenSource.CreateLinkedTokenSource(_extToken, CompletionToken);
                                _token = compound_source.Token;
                            }
                            else if(_monitorCompletionToken) _token =CompletionToken;
                            else if(_extToken.CanBeCanceled) _token=_extToken;
                            try {
                                _checkPoints.Values[cp_pos++]();
                            }
                            catch (OperationCanceledException){
                                if(_extToken.IsCancellationRequested) CanceledExternally=true;
                                throw;
                            }
                            finally {
                                _token=default;
                                compound_source?.Dispose();
                            }
                        }
                        if(_monitorCompletionToken) CompletionToken.ThrowIfCancellationRequested();
                        TResult result = _resultFunc(_position);
                        InCallback=true;
                        ProgressCallback(result, _reportCount ? _count+1 : null);
                        InCallback=false;
                    }
                }
                finally {
                    _ranToEnd=true;
                    _pauseEvent.Set();
                }                
                return _resultFunc(_count);
            }

            public void VoidBody(Action<TResult, Int32?> ProgressCallback, CancellationToken Token)
            {
                TResult result = FuncBody(ProgressCallback,Token);
            }

            void Pause()
            {
                _readyEvent.Reset();
                _pauseEvent.Set();
                _readyEvent.Wait(_token);
            }

            void Fail()
            {
                throw new TestException();
            }

            public Boolean Wait(Int32 TimeOut, SessionProcessRunner<TResult> Runner)
            {
                DateTime start= DateTime.Now;
                Boolean result=_pauseEvent.Wait(TimeOut);
                if(!result || !_ranToEnd) return result;
                Int32 timeout_left = TimeOut==-1? -1 : Math.Max(0, TimeOut-(Int32)((DateTime.Now-start).TotalMilliseconds));
                result=Runner._bkgCompletionTask?.Wait(timeout_left)??false;
                return result;
            }

            public Boolean WaitWithResume(Int32 TimeOut, SessionProcessRunner<TResult> Runner)
            {
                Boolean result = false;
                _pauseEvent.Reset();
                try {
                    result=_readyEvent.Wait(TimeOut, _token);
                }
                catch(OperationCanceledException) {
                    result=true;
                }
                catch { }
                return result && Wait(TimeOut,Runner);
            }

            public void Resume()
            {
                _pauseEvent.Reset();
                _readyEvent.Set();
            }

            public void Dispose()
            {
                _pauseEvent.Dispose();
                _readyEvent.Dispose();
            }
        }

        class TestException : Exception { };

    }
}
