using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace ActiveSession.Tests
{
    public class EnumerableRunnerBaseTests
    {

        public const Int32 PAGE_SIZE = 10;
        static TimeSpan TIMEOUT = TimeSpan.FromSeconds(5);
        
        Boolean CheckRange(IEnumerable<Int32> Range,  Int32 Start, Int32 Length)
        {
            Int32 item_to_compare = Start;
            foreach(Int32 item in Range) {
                if(item_to_compare++ != item) return false;
            }
            return item_to_compare == Start+Length;
        }

        [Fact]
        //Test group: GetAvailable parameter check and interpretation
        public void GetAvailable_Parameters()
        {

            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            //Test starting background in a constructor
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                Assert.True(runner.Started);
                Assert.Equal(RunnerStatus.Stalled, runner.Status);

                //Invalid Advance test
                Assert.Throws<InvalidOperationException>(()=>runner.GetAvailable(-1));
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3);
                //Test interpretation of of Advance==DEFAULT_ADVANCE parameter value
                (result, status, position, exception) = runner.GetAvailable(IRunner.DEFAULT_ADVANCE);
                Assert.True(CheckRange(result, 0, PAGE_SIZE));
                //Test default value for Advance parameter
                (result, status, position, exception) = runner.GetAvailable();
                Assert.True(CheckRange(result, PAGE_SIZE, PAGE_SIZE *2));
                //Default StartPosition test performed in parallel with previous tests
            }
            //StartPosition tests
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3);
                (result, status, position, exception) = runner.GetAvailable(PAGE_SIZE);
                //Test invalid explicit StartPosition value
                Assert.Throws<InvalidOperationException>(() => runner.GetAvailable(StartPosition:PAGE_SIZE*2));
                //Test valid explicit StartPosition value
                (result, status, position, exception) = runner.GetAvailable(StartPosition:PAGE_SIZE);
                Assert.True(CheckRange(result, PAGE_SIZE, PAGE_SIZE * 2));
            }
        }

        [Fact]
        //Test group: GetAvailable in the case of background fetch is in progress
        public void GetAvailable_InProgress()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                //Test while none has been fetched in the background yet
                (result, status, position, exception)=runner.GetAvailable();
                Assert.True(CheckRange(result, 0, 0));
                Assert.Equal(RunnerStatus.Stalled, status);
                Assert.Equal(0, position);
                Assert.Null(exception);

                runner.SimulateBackgroundFetch(PAGE_SIZE*3);
                //Test partialial available data extraction 
                (result, status, position, exception) = runner.GetAvailable(PAGE_SIZE);
                Assert.True(CheckRange(result, 0, PAGE_SIZE));
                Assert.Equal(RunnerStatus.Progressed, status);
                Assert.Equal(PAGE_SIZE, position);
                Assert.Null(exception);
                //Test all available data extraction 
                (result, status, position, exception) = runner.GetAvailable();
                Assert.True(CheckRange(result, PAGE_SIZE, PAGE_SIZE*2));
                Assert.Equal(RunnerStatus.Stalled, status);
                Assert.Equal(PAGE_SIZE*3, position);
                Assert.Null(exception);
            }
        }

        [Fact]
        //Test group: GetAvailable in the case of background fetch was completed
        public void GetAvailable_Completed()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3, true);
                //Test partial available data extraction
                (result, status, position, exception) = runner.GetAvailable(PAGE_SIZE);
                Assert.True(CheckRange(result, 0, PAGE_SIZE));
                Assert.Equal(RunnerStatus.Progressed, status);
                Assert.Equal(PAGE_SIZE, position);
                Assert.Null(exception);
                //Test all available data extraction
                (result, status, position, exception) = runner.GetAvailable();
                Assert.True(CheckRange(result, PAGE_SIZE, 2*PAGE_SIZE));
                Assert.Equal(RunnerStatus.Complete, status);
                Assert.Null(exception);
            }
        }

        [Fact]
        //Test group: GetAvailable in the case of background fetch throws an exception
        public void GetAvailable_Failed()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3, true,new TestException());
                //Test partial available data extraction
                (result, status, position, exception) = runner.GetAvailable(PAGE_SIZE);
                Assert.True(CheckRange(result, 0, PAGE_SIZE));
                Assert.Equal(RunnerStatus.Progressed, status);
                Assert.Equal(PAGE_SIZE, position);
                Assert.Null(exception);
                //Test all available data extraction
                (result, status, position, exception) = runner.GetAvailable();
                Assert.True(CheckRange(result, PAGE_SIZE, 2 * PAGE_SIZE));
                Assert.Equal(RunnerStatus.Failed, status);
                Assert.NotNull(exception);
                Assert.IsType<TestException>(exception);
            }
        }

        [Fact]
        //Test group: GetAvailable after an Abort call with and without some data in the queue
        public void GetAvailable_Abort()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            //Test Abort when background fetch is not completed yet
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3);
                runner.Abort();
                (result, status, position, exception) = runner.GetAvailable(PAGE_SIZE);
                Assert.True(CheckRange(result, 0, 0));
                Assert.Equal(RunnerStatus.Aborted, status);
                Assert.Null(exception);
            }
            //Test Abort when background fetch is complete
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3, true);
                runner.Abort();
                (result, status, position, exception) = runner.GetAvailable(PAGE_SIZE);
                Assert.True(CheckRange(result, 0, 0));
                Assert.Equal(RunnerStatus.Aborted, status);
                Assert.Null(exception);
            }
            //Test Abort when background fetch throwed an error
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3, true, new TestException());
                runner.Abort();
                (result, status, position, exception) = runner.GetAvailable(PAGE_SIZE);
                Assert.True(CheckRange(result, 0, 0));
                Assert.Equal(RunnerStatus.Aborted, status);
                Assert.Null(exception);
            }
        }

        [Fact]
        //Test group: GetAvailable at final stages
        public void GetAvailable_Final()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            //Test GetAvailablle while runner has Complete status
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3, true);
                (result, status, position, exception) = runner.GetAvailable();
                Assert.Equal(RunnerStatus.Complete, status);
                (result, status, position, exception) = runner.GetAvailable();
                Assert.True(CheckRange(result, PAGE_SIZE * 3, 0));
                Assert.Equal(RunnerStatus.Complete, status);
                Assert.Null(exception);
            }
            //Test GetAvailablle while runner has Failed status
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3, true, new TestException());
                (result, status, position, exception) = runner.GetAvailable();
                Assert.Equal(RunnerStatus.Failed, status);
                (result, status, position, exception) = runner.GetAvailable();
                Assert.True(CheckRange(result, PAGE_SIZE * 3, 0));
                Assert.Equal(RunnerStatus.Failed, status);
                Assert.NotNull(exception);
                Assert.IsType<TestException>(exception);
            }
            //Test GetAvailablle while runner has Aborted status
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.Abort();
                (result, status, position, exception) = runner.GetAvailable();
                Assert.True(CheckRange(result, 0, 0));
                Assert.Equal(RunnerStatus.Aborted, status);
                Assert.Null(exception);
            }
        }

        [Fact]
        //Test group: GetAvailable - using data stashed after previous cancellation
        public void GetAvailable_UseStashedData()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                using(CancellationTokenSource cts = new CancellationTokenSource()) {
                    result_task = runner.GetRequiredAsync(3 * PAGE_SIZE, cts.Token).Preserve();
                    Assert.False(result_task.IsCompleted);
                    runner.SimulateBackgroundFetchWithWait(PAGE_SIZE * 2);
                    Assert.False(result_task.IsCompleted);
                    cts.Cancel();
                    Assert.Throws<TaskCanceledException>(() => result_task.GetAwaiter().GetResult());

                    //Test case: more than required stashed data to satisfy request
                    (result, status, position, exception) = runner.GetAvailable(PAGE_SIZE);
                    Assert.True(CheckRange(result, 0, PAGE_SIZE));
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(PAGE_SIZE, position);
                    Assert.Null(exception);
                    //Test case: not enough stashed data to satisfy request but with more data in the queue it's enough
                    runner.SimulateBackgroundFetch(PAGE_SIZE * 2);
                    (result, status, position, exception) = runner.GetAvailable(PAGE_SIZE * 2);
                    Assert.True(CheckRange(result, PAGE_SIZE, PAGE_SIZE * 2));
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(PAGE_SIZE * 3, position);
                    Assert.Null(exception);
                    //Test case: check predictions about data left in the queue
                    (result, status, position, exception) = runner.GetAvailable(PAGE_SIZE * 2);
                    Assert.True(CheckRange(result, PAGE_SIZE * 3, PAGE_SIZE));
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(PAGE_SIZE * 4, position);
                    Assert.Null(exception);
                }
            }
        }

        [Fact]
        //Test group: GetRequiredAsync parameter check and interpretation
        public void GetRequiredAsync_Parameters()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger,false)) {
                Assert.False(runner.Started);
                Assert.Equal(RunnerStatus.NotStarted, runner.Status);

                //Invalid Advance test
                Assert.Throws<InvalidOperationException>(() => runner.GetRequiredAsync(-1));
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3);
                //Test default value for Advance parameter
                (result, status, position, exception) = runner.GetRequiredAsync().Result;
                Assert.True(runner.Started);
                Assert.NotEqual(RunnerStatus.NotStarted, runner.Status);
                Assert.True(CheckRange(result, 0, PAGE_SIZE));
                //Default StartPosition test performed in parallel with previous tests
            }
            //StartPosition tests
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3);
                (result, status, position, exception) = runner.GetRequiredAsync(PAGE_SIZE).Result;
                //Test invalid explicit StartPosition value
                Assert.Throws<InvalidOperationException>(() => runner.GetRequiredAsync(StartPosition: PAGE_SIZE * 2));
                //Test valid explicit StartPosition value
                (result, status, position, exception) = runner.GetRequiredAsync(StartPosition: PAGE_SIZE).Result;
                Assert.True(CheckRange(result, PAGE_SIZE, PAGE_SIZE));
            }
        }

        [Fact]
        //Test group: GetRequiredAsync synchronous-only execution (the queue contains enough data), background work in progress
        public void GetRequiredAsync_InProgressSync() 
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 2);
                //Test synchronous call that leaves some data in the queue
                result_task = runner.GetRequiredAsync(PAGE_SIZE).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.True(CheckRange(result, 0, PAGE_SIZE));
                Assert.Equal(RunnerStatus.Progressed, status);
                Assert.Equal(PAGE_SIZE, position);
                Assert.Null(exception);
                //Test synchronous call that gets all data from the queue while backgrond work is in progress
                result_task = runner.GetRequiredAsync(PAGE_SIZE).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.True(CheckRange(result, PAGE_SIZE, PAGE_SIZE));
                Assert.Equal(RunnerStatus.Stalled, status);
                Assert.Equal(PAGE_SIZE*2, position);
                Assert.Null(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
        }

        [Fact]
        //Test group: GetRequiredAsync synchronous-only execution (the queue contains enough data), background work complete
        public void GetRequiredAsync_CompletedSync()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 2,true);
                //Test synchronous call that leaves some data in the queue
                result_task = runner.GetRequiredAsync(PAGE_SIZE).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.True(CheckRange(result, 0, PAGE_SIZE));
                Assert.Equal(RunnerStatus.Progressed, status);
                Assert.Equal(PAGE_SIZE, position);
                Assert.Null(exception);
                //Test synchronous call that gets more data from the queue while backgrond work is already complete
                result_task = runner.GetRequiredAsync(PAGE_SIZE*2).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.True(CheckRange(result, PAGE_SIZE, PAGE_SIZE));
                Assert.Equal(RunnerStatus.Complete, status);
                Assert.Equal(PAGE_SIZE * 2, position);
                Assert.Null(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
        }

        [Fact]
        //Test group: GetRequiredAsync synchronous-only execution (the queue contains enough data), background work has been failed
        public void GetRequiredAsync_FailedSync()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 2, true, new TestException());
                //Test synchronous call that leaves some data in the queue
                result_task = runner.GetRequiredAsync(PAGE_SIZE).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.True(CheckRange(result, 0, PAGE_SIZE));
                Assert.Equal(RunnerStatus.Progressed, status);
                Assert.Equal(PAGE_SIZE, position);
                Assert.Null(exception);
                //Test synchronous call that gets more data from the queue while backgrond work has been failed
                result_task = runner.GetRequiredAsync(PAGE_SIZE * 2).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.True(CheckRange(result, PAGE_SIZE, PAGE_SIZE));
                Assert.Equal(RunnerStatus.Failed, status);
                Assert.Equal(PAGE_SIZE * 2, position);
                Assert.NotNull(exception);
                Assert.IsType<TestException>(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
        }

        [Fact]
        //Test group: GetRequiredAsync asynchronous execution, background work is in progres
        public void GetRequiredAsync_InProgresAsync()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            Task<RunnerResult<IEnumerable<Int32>>> result_as_task;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                //Test call without any data in the queue
                result_task = runner.GetRequiredAsync(2*PAGE_SIZE).Preserve();
                Assert.False(result_task.IsCompleted);
                //Test outstanding async call state given insufficient data to complete
                runner.SimulateBackgroundFetchWithWait(PAGE_SIZE);
                Assert.False(result_task.IsCompleted);
                //Test outstanding async call state given excess data to complete
                runner.SimulateBackgroundFetchWithWait(PAGE_SIZE*2);
                result_as_task = result_task.AsTask();
                Assert.True(result_as_task.Wait(TIMEOUT));
                Assert.True(result_as_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_as_task.Result;
                Assert.True(CheckRange(result, 0, 2*PAGE_SIZE));
                Assert.Equal(RunnerStatus.Progressed, status);
                Assert.Equal(2*PAGE_SIZE, position);
                Assert.Null(exception);
                //Test call with insufficient data in the queue
                result_task = runner.GetRequiredAsync(2 * PAGE_SIZE).Preserve();
                Assert.False(result_task.IsCompleted);
                //Test outstanding async call state given enough data to complete
                runner.SimulateBackgroundFetchWithWait(PAGE_SIZE);
                result_as_task = result_task.AsTask();
                Assert.True(result_as_task.Wait(TIMEOUT));
                Assert.True(result_as_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_as_task.Result;
                Assert.True(CheckRange(result, PAGE_SIZE*2, PAGE_SIZE*2));
                Assert.Equal(RunnerStatus.Stalled, status);
                Assert.Equal(PAGE_SIZE*4, position);
                Assert.Null(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
        }

        [Fact]
        //Test group: GetRequiredAsync asynchronous execution, background work is in progres
        public void GetRequiredAsync_CompletedAsync() 
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            Task<RunnerResult<IEnumerable<Int32>>> result_as_task;
            //Test outstanding async call state giving less data than requested but complete the background task
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                result_task = runner.GetRequiredAsync(2 * PAGE_SIZE).Preserve();
                Assert.False(result_task.IsCompleted);
                runner.SimulateBackgroundFetchWithWait(PAGE_SIZE, true);
                result_as_task = result_task.AsTask();
                Assert.True(result_as_task.Wait(TIMEOUT));
                Assert.True(result_as_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_as_task.Result;
                Assert.True(CheckRange(result, 0, PAGE_SIZE));
                Assert.Equal(RunnerStatus.Complete, status);
                Assert.Null(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
            //Test outstanding async call state giving more data than requested and complete the background task
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                result_task = runner.GetRequiredAsync(2*PAGE_SIZE).Preserve();
                Assert.False(result_task.IsCompleted);
                runner.SimulateBackgroundFetchWithWait(3*PAGE_SIZE, true);
                result_as_task = result_task.AsTask();
                Assert.True(result_as_task.Wait(TIMEOUT));
                Assert.True(result_as_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_as_task.Result;
                Assert.True(CheckRange(result, 0, 2*PAGE_SIZE));
                Assert.Equal(RunnerStatus.Progressed, status);
                Assert.Null(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
        }

        [Fact]
        //Test group: GetRequiredAsync asynchronous execution, background work failed
        public void GetRequiredAsync_FailedAsync()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            Task<RunnerResult<IEnumerable<Int32>>> result_as_task;
            //Test outstanding async call state giving less data than requested but fail the background task
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                result_task = runner.GetRequiredAsync(2 * PAGE_SIZE).Preserve();
                Assert.False(result_task.IsCompleted);
                runner.SimulateBackgroundFetchWithWait(PAGE_SIZE, true, new TestException());
                result_as_task = result_task.AsTask();
                Assert.True(result_as_task.Wait(TIMEOUT));
                Assert.True(result_as_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_as_task.Result;
                Assert.True(CheckRange(result, 0, PAGE_SIZE));
                Assert.Equal(RunnerStatus.Failed, status);
                Assert.NotNull(exception);
                Assert.IsType<TestException>(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
            //Test outstanding async call state giving more data than requested and fail the background task
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                result_task = runner.GetRequiredAsync(2 * PAGE_SIZE).Preserve();
                Assert.False(result_task.IsCompleted);
                runner.SimulateBackgroundFetchWithWait(3 * PAGE_SIZE, true, new TestException());
                result_as_task = result_task.AsTask();
                Assert.True(result_as_task.Wait(TIMEOUT));
                Assert.True(result_as_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_as_task.Result;
                Assert.True(CheckRange(result, 0, 2 * PAGE_SIZE));
                Assert.Equal(RunnerStatus.Progressed, status);
                Assert.Null(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
        }

        [Fact]
        //Test: GetRequiredAsync cancellation
        public void GetRequiredAsync_Canceled() 
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                using (CancellationTokenSource cts=new CancellationTokenSource()) {
                    result_task = runner.GetRequiredAsync(2 * PAGE_SIZE, cts.Token).Preserve();
                    Assert.False(result_task.IsCompleted);
                    runner.SimulateBackgroundFetchWithWait(PAGE_SIZE);
                    Assert.False(result_task.IsCompleted);
                    cts.Cancel();
                    Assert.Throws<TaskCanceledException>(() => result_task.GetAwaiter().GetResult());
                    Assert.Equal(TaskStatus.Canceled, result_task.AsTask().Status);
                    (result, status, position, exception) = runner.GetAvailable();
                    Assert.True(CheckRange(result, 0, PAGE_SIZE));
                    runner.GetAvailable(); //To check pseudo-lock release
                }
            }
        }

        [Fact]
        //Test: GetRequiredAsync exception in the waiting task has been thrown
        public void GetRequiredAsync_Exception()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                result_task = runner.GetRequiredAsync(2 * PAGE_SIZE).Preserve();
                Assert.False(result_task.IsCompleted);
                runner.SimulateBackgroundFetchWithWait(PAGE_SIZE);
                Assert.False(result_task.IsCompleted);
                runner.SimulateFetchException(new TestException());
                Assert.Throws<TestException>(() => result_task.GetAwaiter().GetResult());
                (result, status, position, exception) = runner.GetAvailable();
                Assert.True(CheckRange(result, 0, PAGE_SIZE));
                runner.GetAvailable(); //To check pseudo-lock release
            }
        }

        [Fact]
        //Test group: GetRequiredAsync - using data stashed due to previous cancellation
        public void GetRequiredAsync_UseStashedData()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            Task<RunnerResult<IEnumerable<Int32>>> result_as_task;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                using(CancellationTokenSource cts = new CancellationTokenSource()) {
                    result_task = runner.GetRequiredAsync(3 * PAGE_SIZE, cts.Token).Preserve();
                    Assert.False(result_task.IsCompleted);
                    runner.SimulateBackgroundFetchWithWait(PAGE_SIZE * 2);
                    Assert.False(result_task.IsCompleted);
                    cts.Cancel();
                    Assert.Throws<TaskCanceledException>(() => result_task.GetAwaiter().GetResult());
                    //Test case: more than required stashed data to satisfy request
                    result_task = runner.GetRequiredAsync(PAGE_SIZE).Preserve();
                    Assert.True(result_task.IsCompletedSuccessfully);
                    (result, status, position, exception) = result_task.Result;
                    Assert.True(CheckRange(result, 0, PAGE_SIZE));
                    Assert.Equal(RunnerStatus.Progressed, status);
                    Assert.Equal(PAGE_SIZE, position);
                    Assert.Null(exception);
                    //Test case: not enough stashed data to satisfy request but with more data in the queue it's enough
                    runner.SimulateBackgroundFetch(PAGE_SIZE);
                    result_task = runner.GetRequiredAsync(PAGE_SIZE * 2).Preserve();
                    Assert.True(result_task.IsCompletedSuccessfully);
                    (result, status, position, exception) = result_task.Result;
                    Assert.True(CheckRange(result, PAGE_SIZE, PAGE_SIZE * 2));
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(PAGE_SIZE * 3, position);
                    Assert.Null(exception);
                }

            }
            //Test case: not enough stashed data and data in the queue to satisfy request but more data is in the queue
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                using(CancellationTokenSource cts = new CancellationTokenSource()) {
                    result_task = runner.GetRequiredAsync(2 * PAGE_SIZE, cts.Token).Preserve();
                    Assert.False(result_task.IsCompleted);
                    runner.SimulateBackgroundFetchWithWait(PAGE_SIZE);
                    Assert.False(result_task.IsCompleted);
                    cts.Cancel();
                    Assert.Throws<TaskCanceledException>(() => result_task.GetAwaiter().GetResult());
                    runner.SimulateBackgroundFetch(PAGE_SIZE);
                    result_task = runner.GetRequiredAsync(PAGE_SIZE * 3).Preserve();
                    Assert.False(result_task.IsCompletedSuccessfully);
                    runner.SimulateBackgroundFetchWithWait(PAGE_SIZE);
                    result_as_task = result_task.AsTask();
                    Assert.True(result_as_task.Wait(TIMEOUT));
                    Assert.True(result_as_task.IsCompletedSuccessfully);
                    (result, status, position, exception) = result_as_task.Result;
                    Assert.True(CheckRange(result, 0, PAGE_SIZE * 3));
                    Assert.Equal(RunnerStatus.Stalled, status);
                    Assert.Equal(PAGE_SIZE * 3, position);
                    Assert.Null(exception);
                }
            }
        }

        [Fact]
        //Test group: GetRequiredAsync after an Abort call with and without some data in the queue
        public void GetRequiredAsync_AbortSync()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            //Test Abort when background fetch is not completed yet
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3);
                runner.Abort();
                result_task = runner.GetRequiredAsync(PAGE_SIZE).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.True(CheckRange(result, 0, 0));
                Assert.Equal(RunnerStatus.Aborted, status);
                Assert.Null(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
            //Test Abort when background fetch is complete
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3, true);
                runner.Abort();
                result_task = runner.GetRequiredAsync(PAGE_SIZE).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.True(CheckRange(result, 0, 0));
                Assert.Equal(RunnerStatus.Aborted, status);
                Assert.Null(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
            //Test Abort when background fetch throwed an error
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3, true, new TestException());
                runner.Abort();
                result_task = runner.GetRequiredAsync(PAGE_SIZE).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.True(CheckRange(result, 0, 0));
                Assert.Equal(RunnerStatus.Aborted, status);
                Assert.Null(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
        }

        [Fact]
        //Test case: an Abort call made while GetRequiredAsync is waiting
        public void GetRequiredAsync_AbortAsync()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            Task<RunnerResult<IEnumerable<Int32>>> result_as_task;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE);
                result_task = runner.GetRequiredAsync(2 * PAGE_SIZE).Preserve();
                Assert.False(result_task.IsCompleted);
                runner.Abort();
                result_as_task = result_task.AsTask();
                Assert.True(result_as_task.Wait(TIMEOUT));
                Assert.True(result_as_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_as_task.Result;
                Assert.True(CheckRange(result, 0, PAGE_SIZE));
                Assert.Equal(RunnerStatus.Aborted, status);
                Assert.Null(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
        }

        [Fact]
        //Test group: GetRequiredAsync at final stages
        public void GetRequiredAsync_Final()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            //Test GetRequiredAsync while runner has Complete status
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3, true);
                result_task = runner.GetRequiredAsync(PAGE_SIZE*3).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.Equal(RunnerStatus.Complete, status);
                result_task = runner.GetRequiredAsync(PAGE_SIZE * 3).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.True(CheckRange(result, PAGE_SIZE * 3, 0));
                Assert.Equal(RunnerStatus.Complete, status);
                Assert.Null(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
            //Test GetRequiredAsync while runner has Failed status
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3, true, new TestException());
                result_task = runner.GetRequiredAsync(PAGE_SIZE * 3).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.Equal(RunnerStatus.Failed, status);
                result_task = runner.GetRequiredAsync(PAGE_SIZE * 3).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.True(CheckRange(result, PAGE_SIZE * 3, 0));
                Assert.Equal(RunnerStatus.Failed, status);
                Assert.NotNull(exception);
                Assert.IsType<TestException>(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
            //Test GetRequiredAsync while runner has Aborted status
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                runner.Abort();
                result_task = runner.GetRequiredAsync(PAGE_SIZE * 3).Preserve();
                Assert.True(result_task.IsCompletedSuccessfully);
                (result, status, position, exception) = result_task.Result;
                Assert.True(CheckRange(result, 0, 0));
                Assert.Equal(RunnerStatus.Aborted, status);
                Assert.Null(exception);
                runner.GetAvailable(); //To check pseudo-lock release
            }
        }

        [Fact]
        //Test group: GetRequiredAsync in parallel with GetAvailable/GetRequiredAsync (pseudo-lock tests)
        public void GetRequiredAsync_Parallel()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            Task<RunnerResult<IEnumerable<Int32>>> result_as_task;
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger)) {
                result_task = runner.GetRequiredAsync(PAGE_SIZE).Preserve();
                Assert.False(result_task.IsCompleted);
                //Test GetAvailable call while GetRequiredAsync is awaiting
                Assert.Throws<InvalidOperationException>(()=>runner.GetAvailable());
                //Test another GetRequiredAsync call while GetRequiredAsync is awaiting
                Assert.Throws<InvalidOperationException>(() => runner.GetRequiredAsync());
                runner.SimulateBackgroundFetch(PAGE_SIZE * 3);
                result_as_task = result_task.AsTask();
                Assert.True(result_as_task.Wait(TIMEOUT));
                Assert.True(result_as_task.IsCompletedSuccessfully);
                //Test another GetRequiredAsync call when GetRequiredAsync has been completed
                runner.GetRequiredAsync(PAGE_SIZE);
                //Test GetAvailable call when GetRequiredAsync has been completed
                runner.GetAvailable();
            }
        }

        [Fact]
        //Test group: Dispose-associated tests
        public void Dispose_Test()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            TestEnumerableRunner runner; 
            runner = new TestEnumerableRunner(logger_mock.Logger);
            //Test case: check that the first Dispose() calls DisposeAsyncCore virtual method
            int called = 0;
            runner.DisposeTaskBody = () => { called++; };
            runner.Dispose();
            Assert.Equal(1, called);
            //Test case: check that the next Dispose() does not calls DisposeAsyncCore virtual method
            runner.Dispose();
            Assert.Equal(1, called);
            //Test case: GetAvailable after disposing throws
            Assert.Throws<ObjectDisposedException>(() => runner.GetAvailable());
            //Test case: GetRequiredAsync after disposing throws
            Assert.Throws<ObjectDisposedException>(() => runner.GetRequiredAsync());
            //Test case: Abort after disposing does not throw
            runner.Abort();
            //Test case: Dispose while GetRequiredAsync is awaiting
            runner = new TestEnumerableRunner(logger_mock.Logger);
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            result_task = runner.GetRequiredAsync();
            runner.Dispose();
            Task result_as_task = result_task.AsTask();
            Assert.Equal(nameof(TestEnumerableRunner), 
                WaitExceptionChecker<ObjectDisposedException>.Check(result_as_task, TIMEOUT).ObjectName);
        }

        [Fact]
        //Test group: DisposeAsync-associated tests
        public void DisposeAsync_Test()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            TestEnumerableRunner runner;
            runner = new TestEnumerableRunner(logger_mock.Logger);
            ValueTask dispose_task = default;
            Task dispose_as_task;
            //Test case: check that check that the first DisposeAsync() calls DisposeAsyncCore virtual method
            using(ManualResetEventSlim dispose_event=new ManualResetEventSlim(false)) {
                runner.DisposeTaskBody = () => { dispose_event.Wait(); };
                dispose_task = runner.DisposeAsync();
                dispose_as_task = dispose_task.AsTask();
                Assert.False(dispose_task.IsCompleted);
                //Test case: second DisposeAsyncCall while disposing
                Assert.Same(dispose_as_task, runner.DisposeAsync().AsTask());
                //Test case: GetAvailable while disposing throws
                Assert.Throws<ObjectDisposedException>(() => runner.GetAvailable());
                //Test case: GetRequiredAsync while disposing throws
                Assert.Throws<ObjectDisposedException>(() => runner.GetRequiredAsync());
                //Test case: Abort while disposing does not throw
                runner.Abort();
                dispose_event.Set();
                Assert.True(dispose_as_task.Wait(TIMEOUT));
                Assert.True(dispose_as_task.IsCompletedSuccessfully);
            }
            //Test case: second DisposeAsyncCall after disposing
            dispose_task = runner.DisposeAsync();
            Assert.True(dispose_task.IsCompletedSuccessfully);
            //Test case: DisposeAsync while GetRequiredAsync is awaiting
            runner = new TestEnumerableRunner(logger_mock.Logger);
            ValueTask<RunnerResult<IEnumerable<Int32>>> result_task;
            result_task = runner.GetRequiredAsync();
            dispose_task = runner.DisposeAsync();
            dispose_as_task = dispose_task.AsTask();
            Assert.True(dispose_as_task.Wait(TIMEOUT));
            Task result_as_task = result_task.AsTask();
            Assert.Equal(nameof(TestEnumerableRunner),
                WaitExceptionChecker<ObjectDisposedException>.Check(result_as_task, TIMEOUT).ObjectName);
        }


        [Fact]
        //Test group: asynchchronous start of the background processing (in GetRequiredAsync only)
        public void GetRequiredAsync_AsyncStartBkgProcessing()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            MockedLogger logger_mock = logger_factory_mock.MonitorLoggerCategory(nameof(EnumerableRunnerBaseTests));
            IEnumerable<Int32> result;
            RunnerStatus status;
            Int32 position;
            Exception? exception;
            Task<RunnerResult<IEnumerable<Int32>>> result_as_task;

            //Test case: synchronously canceled asynchchronous start of the background processing
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger, false)) {
                runner.SetCancelStartBkgSync();
                result_as_task = runner.GetRequiredAsync().AsTask();
                Assert.True(result_as_task.IsCanceled);
            }
            //Test case: exception thrown synchronously during asynchchronous start of the background processing
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger, false)) {
                runner.SetFailStartBkgSync();
                result_as_task = runner.GetRequiredAsync().AsTask();
                Assert.True(result_as_task.IsFaulted);
            }
            //Test case: cancellation during start of the background processing
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger, false, true)) {
                result_as_task = runner.GetRequiredAsync().AsTask();
                Assert.True(runner.WaitForStartBkg());
                Assert.False(runner.Started);
                Assert.False(result_as_task.IsCompleted);
                runner.CancelStartBkg();
                WaitExceptionChecker<TaskCanceledException>.Check(result_as_task, TIMEOUT);
                (result,status,position,exception)=runner.GetAvailable(); //To check pseudo-lock release
                Assert.Empty(result);
            }
            //Test case: exception thrown during start of the background processing
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger, false, true)) {
                result_as_task = runner.GetRequiredAsync().AsTask();
                Assert.True(runner.WaitForStartBkg());
                Assert.False(runner.Started);
                Assert.False(result_as_task.IsCompleted);
                runner.ResumeAndFailStartBkg();
                WaitExceptionChecker<TestException>.Check(result_as_task, TIMEOUT);
                (result, status, position, exception) = runner.GetAvailable(); //To check pseudo-lock release
                Assert.Empty(result);
            }
            //Test case: normal start of background processing, successful asynchronous fetch
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger, false, true)) {
                result_as_task = runner.GetRequiredAsync(PAGE_SIZE).AsTask();
                Assert.True(runner.WaitForStartBkg());
                Assert.False(runner.Started);
                Assert.False(result_as_task.IsCompleted);
                runner.ResumeStartBkg(PAGE_SIZE / 2);
                for(int i = 0; i < 1000 && runner.Queue.Count > 0; i++) Thread.Sleep(100);
                Assert.Empty(runner.Queue);
                Assert.False(result_as_task.IsCompleted);
                runner.SimulateBackgroundFetch(PAGE_SIZE);
                Assert.True(result_as_task.Wait(TIMEOUT));
                (result, status, position, exception) = result_as_task.Result;
                CheckRange(result, 0, PAGE_SIZE);
                (result, status, position, exception) = runner.GetAvailable(); //To check pseudo-lock release
                CheckRange(result, PAGE_SIZE, PAGE_SIZE/2);
            }
            //Test case: normal start of background processing, successful synchronous fetch
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger, false, true)) {
                result_as_task = runner.GetRequiredAsync(PAGE_SIZE).AsTask();
                Assert.True(runner.WaitForStartBkg());
                Assert.False(runner.Started);
                Assert.False(result_as_task.IsCompleted);
                runner.ResumeStartBkg(PAGE_SIZE * 2);
                Assert.True(result_as_task.Wait(TIMEOUT));
                (result, status, position, exception) = result_as_task.Result;
                CheckRange(result, 0, PAGE_SIZE);
                (result, status, position, exception) = runner.GetAvailable(); //To check pseudo-lock release
                CheckRange(result, PAGE_SIZE, PAGE_SIZE);
            }
            //Test case: normal start of background processing, cancellation of asynchronous fetch
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger, false, true)) {
                using(CancellationTokenSource cts = new CancellationTokenSource()) {
                    result_as_task = runner.GetRequiredAsync(PAGE_SIZE, cts.Token).AsTask();
                    Assert.True(runner.WaitForStartBkg());
                    Assert.False(runner.Started);
                    Assert.False(result_as_task.IsCompleted);
                    runner.ResumeStartBkg(PAGE_SIZE / 2);
                    for(int i = 0; i < 1000 && runner.Queue.Count > 0; i++) Thread.Sleep(100);
                    Assert.Empty(runner.Queue);
                    Assert.False(result_as_task.IsCompleted);
                    cts.Cancel();
                    WaitExceptionChecker<TaskCanceledException>.Check(result_as_task, TIMEOUT);
                    (result, status, position, exception) = runner.GetAvailable(); //To check pseudo-lock release
                    CheckRange(result, 0, PAGE_SIZE / 2);
                }
            }
            //Test case: normal start of background processing, exception thrown during asynchronous fetch
            using(TestEnumerableRunner runner = new TestEnumerableRunner(logger_mock.Logger, false, true)) {
                result_as_task = runner.GetRequiredAsync(PAGE_SIZE).AsTask();
                Assert.True(runner.WaitForStartBkg());
                Assert.False(runner.Started);
                Assert.False(result_as_task.IsCompleted);
                runner.ResumeStartBkg(PAGE_SIZE / 2);
                for(int i = 0; i < 1000 && runner.Queue.Count > 0; i++) Thread.Sleep(100);
                Assert.Empty(runner.Queue);
                Assert.False(result_as_task.IsCompleted);
                runner.SimulateFetchException(new TestException());
                WaitExceptionChecker<TestException>.Check(result_as_task, TIMEOUT);
                (result, status, position, exception) = runner.GetAvailable(); //To check pseudo-lock release
                CheckRange(result, 0, PAGE_SIZE / 2);
            }
        }


        [Fact]
        //Test group: check parameter passing
        public void Constructor_Params()
        {
            IEnumerable<Int32> result;
            Int32 qsize;
            Int32 advance;

            //Test case: default parameter values and default options
            using(TestEnumerableParamRunner runner = new TestEnumerableParamRunner(new ActiveSessionOptions(), null, null)) {
                Assert.Equal(ActiveSessionConstants.ENUM_DEFAULT_QUEUE_SIZE, runner.FillQueueToCapacity());
                (result, _, _, _) = runner.GetRequiredAsync().Result;
                Assert.Equal(ActiveSessionConstants.ENUM_DEFAULT_ADVANCE, result.Count());
            }
            //Test case: default parameter values and specified options
            qsize = 2048;
            advance = 10;
            using(TestEnumerableParamRunner runner = new TestEnumerableParamRunner(
                    new ActiveSessionOptions { DefaultEnumerableQueueSize=qsize, DefaultEnumerableAdvance=advance }, null, null)) {
                Assert.Equal(qsize, runner.FillQueueToCapacity());
                (result, _, _, _) = runner.GetRequiredAsync().Result;
                Assert.Equal(advance, result.Count());
            }
            //Test case: specified parameter values and default options
            using(TestEnumerableParamRunner runner = new TestEnumerableParamRunner(new ActiveSessionOptions(), advance, qsize)) {
                Assert.Equal(qsize, runner.FillQueueToCapacity());
                (result, _, _, _) = runner.GetRequiredAsync().Result;
                Assert.Equal(advance, result.Count());
            }
        }


        ////
        // Auxilary classes
        ////
        class TestException : Exception {}

        class TestEnumerableRunner : EnumerableRunnerBase<int>
        {

            Int32 _progress = 0;
            readonly ManualResetEventSlim _resultEvent = new ManualResetEventSlim(false);
            readonly ManualResetEventSlim _fetchEvent = new ManualResetEventSlim(true);
            readonly Boolean _pauseBkgStart=false;
            readonly ManualResetEventSlim _testEvent = new ManualResetEventSlim(false);
            readonly ManualResetEventSlim _proceedEvent = new ManualResetEventSlim(false);
            readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private Task? _startBkgTask=null;
            Boolean _started = false;
            Boolean _disposing = false;
            volatile Task? _fetchingTask = null;

            public Boolean Started { get => _started; }
            public Boolean DisposeCalled { get => _disposing; }
            public Action? DisposeTaskBody = null;

            Int32 _maxAdvance;
            List<Int32>? _result;
            CancellationToken _cancellationToken;
            Exception? _fetchException;
            Boolean _startBkgFaulted=false;
            Boolean _startBkgFaultedSync = false;
            private Boolean _startBkgCancelededSync=false;

            public TestEnumerableRunner(ILogger? Logger = null, Boolean StartInCostructor=true, Boolean PauseBkgStart=false) 
                : base(null, true, default(RunnerId), Logger, PAGE_SIZE, 1024) 
            {
                if(StartInCostructor) this.StartRunning();
                _pauseBkgStart = PauseBkgStart;
            }

            public void SimulateBackgroundFetchWithWait(Int32 Advance, Boolean IsTheLast = false, Exception? BackgroundException = null)
            {
                _fetchEvent.Reset();
                try {
                    SimulateBackgroundFetch(Advance, IsTheLast, BackgroundException);
                    if(!_fetchEvent.Wait(TIMEOUT)) throw new TimeoutException();
                }
                catch(Exception) {
                    _fetchEvent.Set();
                    throw;
                }
            }

            public void SimulateBackgroundFetch(Int32 Advance, Boolean IsTheLast=false, Exception? BackgroundException=null)
            {
                if(_disposing) throw new ObjectDisposedException(this.GetType().FullName);
                if(!Monitor.TryEnter(this,TIMEOUT)) throw new TimeoutException();
                try {
                    if(Status.IsFinal()) throw new InvalidOperationException("The runner is already completed.");
                    for(Int32 i = 0; i < Advance; i++) Queue.Add(_progress++);
                    if(IsTheLast) {
                        Queue.CompleteAdding();
                        if(BackgroundException != null) Exception = BackgroundException;
                    }
                    _resultEvent.Set();

                }
                finally {
                    if(Monitor.IsEntered(this)) Monitor.Exit(this);
                }            
            }

            public void SimulateFetchException(Exception Exception)
            {
                _fetchEvent.Reset();
                try {
                    if(!Monitor.TryEnter(this, TIMEOUT)) throw new TimeoutException();
                    try {
                        _fetchException = Exception;
                        _resultEvent.Set();

                    }
                    finally {
                        if(Monitor.IsEntered(this)) Monitor.Exit(this);
                    }
                    if(!_fetchEvent.Wait(TIMEOUT)) throw new TimeoutException();

                }
                catch(Exception) {
                    _fetchEvent.Set();
                    throw;
                }
            }

            public Boolean WaitForStartBkg()
            {
                return _startBkgTask==null || _startBkgTask.IsCompleted || _testEvent.Wait(5000);
            }

            public void ResumeStartBkg(Int32 PrefechCount=0)
            {
                if(PrefechCount > 0) SimulateBackgroundFetch(PrefechCount);
                _testEvent.Reset();
                _proceedEvent.Set();
            }

            public void CancelStartBkg()
            {
                _cts.Cancel();
            }

            public void ResumeAndFailStartBkg()
            {
                _startBkgFaulted = true;
                ResumeStartBkg();
            }

            public void SetFailStartBkgSync()
            {
                _startBkgFaultedSync = true;
            }

            public void SetCancelStartBkgSync()
            {
                _startBkgCancelededSync = true;
            }

            protected internal override Task FetchRequiredAsync(Int32 MaxAdvance, List<Int32> Result, CancellationToken Token)
            {
                _maxAdvance = MaxAdvance;
                _result = Result;
                _cancellationToken = Token;
                _fetchException = null;
                return _fetchingTask=Task.Run(FetchBody, Token);
            }

            protected internal override Task StartBackgroundProcessingAsync()
            {
                Action pause= () => { _proceedEvent.Reset(); _testEvent.Set(); 
                    _proceedEvent.Wait(_cts.Token); if(_startBkgFaulted) throw new TestException(); };

                _startBkgTask = _startBkgFaultedSync?Task.FromException(new TestException()):
                    _startBkgCancelededSync?Task.FromCanceled(new CancellationToken(true)):
                        _pauseBkgStart ?Task.Run(pause, _cts.Token) :Task.CompletedTask;
                _startBkgTask.ContinueWith(SetStarted,TaskContinuationOptions.ExecuteSynchronously);
                return _startBkgTask;
            }

            void SetStarted(Task _)
            {
                _started = true;
            }

            protected override async Task DisposeAsyncCore()
            {
                _disposing = true;
                _fetchException ??= new ObjectDisposedException("TestEnumerableRunner@mangled");
                _resultEvent.Set();
                //Thread.Sleep(0);
                Task.Yield().GetAwaiter().GetResult();
                if(_fetchingTask != null) 
                    try {
                        await _fetchingTask;
                    }
                    catch { }
                _resultEvent.Dispose();
                _fetchEvent.Dispose();
                _cts.Cancel();
                try {
                    if(_startBkgTask != null && !_startBkgTask.IsCompleted)   await _startBkgTask;
                }
                catch { }
                _cts.Dispose();
                _testEvent.Dispose();
                _proceedEvent.Dispose();
                await base.DisposeAsyncCore();
                if(DisposeTaskBody != null) await Task.Run(DisposeTaskBody!);
            }

            void FetchBody()
            {
                try {
                    if(!_started) throw new InvalidOperationException("Background task was not started yet.");
                    while(!Status.IsFinal() && _result != null && _result!.Count < _maxAdvance) {
                        _resultEvent.Wait(_cancellationToken);
                        Monitor.Enter(this);
                        try {
                            if(_fetchException != null) throw _fetchException!;
                            Int32 item;
                            while(_result!.Count < _maxAdvance && Queue.TryTake(out item)) _result!.Add(item);
                            if(_result!.Count < _maxAdvance && !Queue.IsAddingCompleted) {
                                _resultEvent.Reset();
                            }
                            else {
                                _result = null;
                            }
                        }
                        finally {
                            _fetchEvent.Set();
                            Monitor.Exit(this);
                        }
                    }
                }
                finally {
                    _fetchingTask = null;
                }
            }
        }

        class TestEnumerableParamRunner : EnumerableRunnerBase<int>
        {
            public TestEnumerableParamRunner(ActiveSessionOptions Options, Int32? DefaultAdvance, Int32? QueueSize) 
                : base(null, true, default, null, new ActiveSessionOptionsSnapshot(Options), DefaultAdvance, QueueSize) { }

            protected internal override Task FetchRequiredAsync(Int32 MaxAdvance, List<Int32> Result, CancellationToken Token)
            {
                throw new NotImplementedException();
            }

            protected internal override Task StartBackgroundProcessingAsync() { return Task.CompletedTask; }

            public Int32 FillQueueToCapacity()
            {
                StartRunning(RunnerStatus.Progressed);
                Int32 count = 0;
                while(Queue.TryAdd(count, 100)) count++;
                return count;
            }
        }

        static class WaitExceptionChecker<TException> where TException : Exception
        {
            public static TException Check(Task Task, TimeSpan Timeout)
            {
                AggregateException e = Assert.Throws<AggregateException>(() => Task.Wait(Timeout));
                Assert.Single(e.InnerExceptions);
                return Assert.IsType<TException>(e.InnerExceptions[0]);
            }
        }
    }
}
