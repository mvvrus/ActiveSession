using MVVrus.AspNetCore.ActiveSession.StdRunner;
using Microsoft.Extensions.Logging;
namespace ActiveSession.Tests
{
    public class TimeSeriesRunnerTests
    {
        //Tests for TimeSeriesRunner class are needed to be performed only over the TimeSeriesAsyncEnumerable auxilary class

        static TimeSpan INTERVAL = TimeSpan.FromMilliseconds(500);
        const Int32 TIMEOUT = 5000;
        const Int32 TOLERANCE_MS = 250;
        static TimeSpan TOTAL_TOLERANCE = TimeSpan.FromMilliseconds(600);

        //Test group: passing invalid arguments to the constructor
        [Fact]
        public void TimeSeriesAsyncEnumerableArguments()
        {
            //Test case: pass null as a Gauge delegate
            ArgumentNullException exception_null = Assert.Throws<ArgumentNullException>(
                ()=>new TimeSeriesRunner<TimeSpan>.TimeSeriesAsyncEnumerable(null!, INTERVAL, 1));
            Assert.Equal("Gauge", exception_null.ParamName);
            //Test case: pass a non-positive value as Interval
            ArgumentOutOfRangeException exception;
            exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => new TimeSeriesRunner<TimeSpan>.TimeSeriesAsyncEnumerable((new Timer()).Elapsed, 
                TimeSpan.Zero, 1));
            //Test case: pass a non-positive value as Count
            Assert.Equal("Interval", exception.ParamName);
            exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => new TimeSeriesRunner<TimeSpan>.TimeSeriesAsyncEnumerable((new Timer()).Elapsed,
                INTERVAL, 0));
            Assert.Equal("Count", exception.ParamName);
        }

        //Test case: timer with finite count
        [Fact]
        public void FiniteTimer()
        {
            Int32 COUNT = 10;
            TimeSeriesRunner<TimeSpan>.TimeSeriesAsyncEnumerable timer = 
                new TimeSeriesRunner<TimeSpan>.TimeSeriesAsyncEnumerable((new Timer()).Elapsed, INTERVAL, COUNT);
            Int32 count = 0;
            DateTime moment=default;
            TimeSpan elapsed=default;
            Boolean moved = false;
            IAsyncEnumerator<(DateTime,TimeSpan)> timer_enumerator = timer.GetAsyncEnumerator();
            DateTime? start = null;
            try {
                do {
                    Task<Boolean> move_task = timer_enumerator.MoveNextAsync().AsTask();
                    Assert.True(move_task.Wait(TIMEOUT));
                    moved=move_task.Result;
                    if(moved) {
                        (moment, elapsed)=timer_enumerator.Current;
                        if(!start.HasValue) start=moment;
                        Assert.True(Math.Abs((moment-(start.Value+elapsed)).TotalMilliseconds)<TOLERANCE_MS);
                        count++;
                    }
                } while(moved) ;
                Assert.Equal(COUNT, count);
                Assert.NotNull(start);
                Assert.InRange(moment-start.Value, INTERVAL*(COUNT-1)-TOTAL_TOLERANCE, INTERVAL*(COUNT-1)+TOTAL_TOLERANCE);
            }
            finally {
                timer_enumerator.DisposeAsync().AsTask().Wait();
            }
        }

        //Test case: timer with unlimited count and cancellation
        [Fact]
        public void InfiniteTimer()
        {
            Int32 COUNT = 10;
            TimeSeriesRunner<TimeSpan>.TimeSeriesAsyncEnumerable timer =
                new TimeSeriesRunner<TimeSpan>.TimeSeriesAsyncEnumerable((new Timer()).Elapsed, INTERVAL, null);
            Int32 count = 0;
            DateTime moment = default;
            TimeSpan elapsed = default;
            Boolean moved = false;
            DateTime? start = null;
            using(CancellationTokenSource cts=new CancellationTokenSource()) {
                IAsyncEnumerator<(DateTime, TimeSpan)> timer_enumerator = timer.GetAsyncEnumerator(cts.Token);
                try {
                    do {
                        Task<Boolean> move_task = timer_enumerator.MoveNextAsync().AsTask();
                        if(count==COUNT) cts.Cancel();
                        try {
                            Assert.True(move_task.Wait(TIMEOUT));
                            moved=move_task.Result;
                        }
                        catch (AggregateException e) {
                            if(count==COUNT && e.InnerExceptions.Count==1 && e.InnerExceptions[0] is TaskCanceledException) moved=false;
                            else throw;
                        }
                        if(moved) {
                            (moment, elapsed)=timer_enumerator.Current;
                            if(!start.HasValue) start=moment;
                            Assert.True(Math.Abs((moment-(start.Value+elapsed)).TotalMilliseconds)<TOLERANCE_MS);
                            count++;
                        }
                    } while(moved);
                    Assert.Equal(COUNT, count);
                    Assert.NotNull(start);
                    Assert.InRange(moment-start.Value, INTERVAL*(COUNT-1)-TOTAL_TOLERANCE, INTERVAL*(COUNT-1)+TOTAL_TOLERANCE);
                }
                finally {
                    timer_enumerator.DisposeAsync().AsTask().Wait();
                }
            }        
        }

        //Test case: TimeSeriesRunner constructor using a tuple of two values (timer with unlimited count)
        [Fact]
        public void TwoValuesTupleConstructor()
        {

            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            ActiveSessionOptionsSnapshot options = new ActiveSessionOptionsSnapshot(new ActiveSessionOptions());
            Int32 COUNT = 10;
            TimeSeriesRunner<TimeSpan> runner;

            using(runner=new TimeSeriesRunner<TimeSpan>((new Timer().Elapsed, INTERVAL), default, options,
                logger_factory_mock.LoggerFactory.CreateLogger<TimeSeriesRunner<TimeSpan>>())) {
                var result_task = runner.GetRequiredAsync(COUNT).AsTask();
                Assert.True(result_task.Wait((Int32)INTERVAL.TotalMilliseconds*COUNT*2));
                (var result, RunnerStatus status, Int32 position, Exception? exception) = result_task.Result;
                Assert.True(status.IsRunning());
                Int32 count = 0;
                TimeSpan last_delay = TimeSpan.Zero;
                foreach(var t in result) {
                    (_, last_delay)=t;
                    count++;
                }
                Assert.Equal(COUNT, count);
                Assert.InRange(last_delay, INTERVAL*(COUNT-1)-TOTAL_TOLERANCE, INTERVAL*(COUNT-1)+TOTAL_TOLERANCE);
                Assert.Equal(COUNT, position);
                Assert.Null(exception);
            }
        }

        //Test case: TimeSeriesRunner constructor using a tuple of three values (timer with finite count)
        [Fact]
        public void ThreeValuesTupleConstructor()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            ActiveSessionOptionsSnapshot options = new ActiveSessionOptionsSnapshot(new ActiveSessionOptions());
            Int32 COUNT = 10;
            TimeSeriesRunner<TimeSpan> runner;

            using(runner=new TimeSeriesRunner<TimeSpan>((new Timer().Elapsed, INTERVAL, COUNT),default,options,
                logger_factory_mock.LoggerFactory.CreateLogger<TimeSeriesRunner<TimeSpan>>())) {
                var result_task = runner.GetRequiredAsync(COUNT).AsTask();
                Assert.True(result_task.Wait((Int32)INTERVAL.TotalMilliseconds*COUNT*2));
                (var result, RunnerStatus status, Int32 position, Exception? exception) = result_task.Result;
                Assert.Equal(RunnerStatus.Complete, status);
                Int32 count=0;
                TimeSpan last_delay=TimeSpan.Zero;
                foreach(var t in result) {
                    (_, last_delay)=t;
                    count++;
                }
                Assert.Equal(COUNT, count);
                Assert.InRange(last_delay, INTERVAL*(COUNT-1)-TOTAL_TOLERANCE, INTERVAL*(COUNT-1)+TOTAL_TOLERANCE);
                Assert.Equal(COUNT, position);
                Assert.Null(exception);
            }
        }

        //Test case: TimeSeriesRunner constructor using a TimeSeriesParam instance
        [Fact]
        public void TimeSeriesParamConstructor()
        {
            MockedLoggerFactory logger_factory_mock = new MockedLoggerFactory();
            ActiveSessionOptionsSnapshot options = new ActiveSessionOptionsSnapshot(new ActiveSessionOptions());
            Int32 COUNT = 10;
            TimeSeriesRunner<TimeSpan> runner;

            using(runner=new TimeSeriesRunner<TimeSpan>( new TimeSeriesParams<TimeSpan> { 
                Gauge=new Timer().Elapsed, Interval=INTERVAL, Count=COUNT, DefaultAdvance=COUNT 
            }, default, options, logger_factory_mock.LoggerFactory.CreateLogger<TimeSeriesRunner<TimeSpan>>())) {
                var result_task = runner.GetRequiredAsync().AsTask();
                Assert.True(result_task.Wait((Int32)INTERVAL.TotalMilliseconds*COUNT*2));
                (var result, RunnerStatus status, Int32 position, Exception? exception) = result_task.Result;
                Assert.Equal(RunnerStatus.Complete, status);
                Int32 count = 0;
                TimeSpan last_delay = TimeSpan.Zero;
                foreach(var t in result) {
                    (_, last_delay)=t;
                    count++;
                }
                Assert.Equal(COUNT, count);
                Assert.InRange(last_delay, INTERVAL*(COUNT-1)-TOTAL_TOLERANCE, INTERVAL*(COUNT-1)+TOTAL_TOLERANCE);
                Assert.Equal(COUNT, position);
                Assert.Null(exception);
            }
        }

        class Timer
        {
            DateTime _start = default;

            public TimeSpan Elapsed()
            {
                DateTime now = DateTime.Now;
                if(_start == default) _start=now;
                return now-_start;
            }
        }
    }
}
