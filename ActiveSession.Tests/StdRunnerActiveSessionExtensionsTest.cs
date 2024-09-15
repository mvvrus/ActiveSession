using Microsoft.AspNetCore.Http;
using MVVrus.AspNetCore.ActiveSession.StdRunner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class StdRunnerActiveSessionExtensionsTest
    {
        const Int32 RUNNER_NUMBER = 42;

        void VerifyRunnerCreation<TRequest,TResult>(SessionMockForRunner<TRequest, TResult> test_setup, 
            IRunner<TResult> runner, Int32 number)
        {
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<TResult>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        void TestProc<TRequest,TResult>(TRequest Param, Func<IActiveSession, TRequest, KeyedRunner<TResult>> RunnerCreator)
        {
            using(SessionMockForRunner<TRequest, TResult> test_setup = new()) {
                (IRunner<TResult> runner, Int32 number) = RunnerCreator(test_setup.Session, Param);
                VerifyRunnerCreation(test_setup, runner, number);
            }
        }

        void TestProc<TRequest1, TRequest2,TResult>(TRequest1 Param1, TRequest2 Param2, Func<IActiveSession, TRequest1, TRequest2, KeyedRunner<TResult>> RunnerCreator)
        {
            using(SessionMockForRunner<(TRequest1, TRequest2), TResult> test_setup = new()) {
                (IRunner<TResult> runner, Int32 number) = RunnerCreator(test_setup.Session, Param1, Param2);
                VerifyRunnerCreation(test_setup, runner, number);
            }
        }

        void TestProc<TRequest1, TRequest2, TRequest3, TResult>(TRequest1 Param1, TRequest2 Param2, TRequest3 Param3,
            Func<IActiveSession, TRequest1, TRequest2, TRequest3, KeyedRunner<TResult>> RunnerCreator)
        {
            using(SessionMockForRunner<(TRequest1, TRequest2, TRequest3), TResult> test_setup = new()) {
                (IRunner<TResult> runner, Int32 number) = RunnerCreator(test_setup.Session, Param1, Param2, Param3);
                VerifyRunnerCreation(test_setup, runner, number);
            }
        }

        void TestProcExclusive<TRequest, TResult>(TRequest Param, Func<IActiveSession, TRequest, IDisposable, KeyedRunner<TResult>> RunnerCreator)
        {
            using(SessionMockForRunner<TRequest, TResult> test_setup = new()) {
                (IRunner<TResult> runner, Int32 number) = RunnerCreator(test_setup.Session, Param, test_setup.GetAccessor());
                VerifyRunnerCreation(test_setup, runner, number);
                test_setup.VerifyExclusive();
            }
        }

        void TestProcExclusive<TRequest1, TRequest2, TResult>(TRequest1 Param1, TRequest2 Param2, Func<IActiveSession, TRequest1, TRequest2, IDisposable, KeyedRunner<TResult>> RunnerCreator)
        {
            using(SessionMockForRunner<(TRequest1, TRequest2), TResult> test_setup = new()) {
                (IRunner<TResult> runner, Int32 number) = RunnerCreator(test_setup.Session, Param1, Param2, test_setup.GetAccessor());
                VerifyRunnerCreation(test_setup, runner, number);
                test_setup.VerifyExclusive();
            }
        }

        void TestProcExclusive<TRequest1, TRequest2, TRequest3, TResult>(TRequest1 Param1, TRequest2 Param2, TRequest3 Param3, Func<IActiveSession, TRequest1, TRequest2, TRequest3, IDisposable, KeyedRunner<TResult>> RunnerCreator)
        {
            using(SessionMockForRunner<(TRequest1, TRequest2, TRequest3), TResult> test_setup = new()) {
                (IRunner<TResult> runner, Int32 number) = RunnerCreator(test_setup.Session, Param1, Param2, Param3, test_setup.GetAccessor());
                VerifyRunnerCreation(test_setup, runner, number);
                test_setup.VerifyExclusive();
            }
        }

        [Fact]
        public void CreateSequenceRunner_Sync()
        {
            TestProc((IEnumerable<String>)new String[] { }, (sess,param) => sess.CreateSequenceRunner(param, default!));
            TestProcExclusive((IEnumerable<String>)new String[] { }, 
                (sess, param, accessor) => sess.CreateSequenceRunner(param, default!, accessor));
        }

        [Fact]
        public void CreateSequenceRunner_SyncParam()
        {
            TestProc(new EnumAdapterParams<String> { Source = new String[] { } }, (sess, param) => sess.CreateSequenceRunner(param, default!));
            TestProcExclusive(new EnumAdapterParams<String> { Source = new String[] { } },
                (sess, param, accessor) => sess.CreateSequenceRunner(param, default!, accessor));
        }

        [Fact]
        public void CreateSequenceRunner_Async()
        {
            TestProc(new String[] { }.AsAsyncEnumerable(), (sess, param) => sess.CreateSequenceRunner(param, default!));
            TestProcExclusive(new String[] { }.AsAsyncEnumerable(),
                (sess, param, accessor) => sess.CreateSequenceRunner(param, default!, accessor));
        }

        [Fact]
        public void CreateSequenceRunner_AsyncParam()
        {
            TestProc(new AsyncEnumAdapterParams<String> { Source = new String[] { }.AsAsyncEnumerable() }, (sess, param) => sess.CreateSequenceRunner(param, default!));
            TestProcExclusive(new AsyncEnumAdapterParams<String> { Source = new String[] { }.AsAsyncEnumerable() },
                (sess, param, accessor) => sess.CreateSequenceRunner(param, default!, accessor));
        }

        [Fact]
        public void CreateTimeSeriesRunner_Param()
        {
            TestProc(new TimeSeriesParams<String> { Gauge= () => DateTime.Now.ToString(), Interval=TimeSpan.FromSeconds(10) }, (sess, param) => sess.CreateTimeSeriesRunner(param, default!));
            TestProcExclusive(new TimeSeriesParams<String> { Gauge= () => DateTime.Now.ToString(), Interval=TimeSpan.FromSeconds(10) },
                (sess, param, accessor) => sess.CreateTimeSeriesRunner(param, default!, accessor));
        }

        [Fact]
        public void CreateTimeSeriesRunner_Infinite()
        {
            TestProc(() => DateTime.Now.ToString(), TimeSpan.FromSeconds(10), (sess, param1, param2) => sess.CreateTimeSeriesRunner<String>(param1, param2, default!));
            TestProcExclusive(() => DateTime.Now.ToString(), TimeSpan.FromSeconds(10),
                (sess, param1, param2, accessor) => sess.CreateTimeSeriesRunner(param1, param2, default!, accessor));
        }

        [Fact]
        public void CreateTimeSeriesRunner_Finite()
        {
            TestProc(() => DateTime.Now.ToString(), TimeSpan.FromSeconds(10), 6, (sess, param1, param2, param3) => sess.CreateTimeSeriesRunner<String>(param1, param2, param3, default!));
            TestProcExclusive(() => DateTime.Now.ToString(), TimeSpan.FromSeconds(10), 6,
                (sess, param1, param2, param3, accessor) => sess.CreateTimeSeriesRunner(param1, param2, param3, default!, accessor));
        }

        [Fact]
        public void CreateSessionProcessRunner_BodyWithResult()
        {
            TestProc<Func<Action<String, Int32?>, CancellationToken, String>, String>(
                (_, _) => "Hello!", (sess, param) => sess.CreateSessionProcessRunner(param, default!));
            TestProcExclusive<Func<Action<String, Int32?>, CancellationToken, String>, String>(
                (_, _) => "Hello!", (sess, param, accessor) => sess.CreateSessionProcessRunner(param, default!, accessor));
        }

        [Fact]
        public void CreateSessionProcessRunner_BodyWithoutResult()
        {
            TestProc<Action<Action<String, Int32?>, CancellationToken>, String>(
                (_, _) => { }, (sess, param) => sess.CreateSessionProcessRunner(param, default!));
            TestProcExclusive<Action<Action<String, Int32?>, CancellationToken>, String>(
                (_, _) => { }, (sess, param, accessor) => sess.CreateSessionProcessRunner(param, default!, accessor));
        }

        [Fact]
        public void CreateSessionProcessRunner_CreatorWithResult()
        {
            TestProc<Func<Action<String, Int32?>, CancellationToken, Task<String>>, String>(
                async (_, _) => await Task.FromResult("Hello!"), (sess, param) => sess.CreateSessionProcessRunner(param, default!));
            TestProcExclusive<Func<Action<String, Int32?>, CancellationToken, Task<String>>, String>(
                async (_, _) => await Task.FromResult("Hello!"), (sess, param, accessor) => sess.CreateSessionProcessRunner(param, default!, accessor));
        }

        [Fact]
        public void CreateSessionProcessRunner_CreatorWithoutResult()
        {
            TestProc<Func<Action<String, Int32?>, CancellationToken, Task>, String>(
                async (_, _) => await Task.CompletedTask, (sess, param) => sess.CreateSessionProcessRunner(param, default!));
            TestProcExclusive<Func<Action<String, Int32?>, CancellationToken, Task>, String>(
                async (_, _) => await Task.CompletedTask, (sess, param, accessor) => sess.CreateSessionProcessRunner(param, default!, accessor));
        }

        [Fact]
        public void CreateSessionProcessRunner_BodyWithResultExtCts()
        {
            TestProc<Func<Action<String, Int32?>, CancellationToken, String>, CancellationTokenSource, Boolean, String>(
                (_, _) => "Hello!", null!, false, (sess, param1, param2, param3) => sess.CreateSessionProcessRunner(param1, default!, param2, param3));
            TestProcExclusive<Func<Action<String, Int32?>, CancellationToken, String>, CancellationTokenSource, Boolean, String>(
                (_, _) => "Hello!", null!, false, (sess, param1, param2, param3, accessor) => sess.CreateSessionProcessRunner(param1, default!, param2, param3, accessor));
        }

        [Fact]
        public void CreateSessionProcessRunner_BodyWithoutResulExtCtst()
        {
            TestProc<Action<Action<String, Int32?>, CancellationToken>, CancellationTokenSource, Boolean, String>(
                (_, _) => { }, null!, false, (sess, param1, param2, param3) => sess.CreateSessionProcessRunner(param1, default!, param2, param3));
            TestProcExclusive<Action<Action<String, Int32?>, CancellationToken>, CancellationTokenSource, Boolean, String>(
                (_, _) => { }, null!, false, (sess, param1, param2, param3, accessor) => sess.CreateSessionProcessRunner(param1, default!, param2, param3, accessor));
        }

        [Fact]
        public void CreateSessionProcessRunner_CreatorWithResultExtCts()
        {
            TestProc<Func<Action<String, Int32?>, CancellationToken, Task<String>>, CancellationTokenSource, Boolean, String>(
                async (_, _) => await Task.FromResult("Hello!"), null!, false, (sess, param1, param2, param3) => sess.CreateSessionProcessRunner(param1, default!, param2, param3));
            TestProcExclusive<Func<Action<String, Int32?>, CancellationToken, Task<String>>, CancellationTokenSource, Boolean, String>(
                async (_, _) => await Task.FromResult("Hello!"), null!, false, (sess, param1, param2, param3, accessor) => sess.CreateSessionProcessRunner(param1, default!, param2, param3, accessor));
        }

        [Fact]
        public void CreateSessionProcessRunner_CreatorWithoutResultExtCts()
        {
            TestProc<Func<Action<String, Int32?>, CancellationToken, Task>, CancellationTokenSource, Boolean, String>(
                async (_, _) => await Task.CompletedTask, null!, false, (sess, param1, param2, param3) => sess.CreateSessionProcessRunner(param1, default!, param2, param3));
            TestProcExclusive<Func<Action<String, Int32?>, CancellationToken, Task>, CancellationTokenSource, Boolean, String>(
                async (_, _) => await Task.CompletedTask, null!, false, (sess, param1, param2, param3, accessor) => sess.CreateSessionProcessRunner(param1, default!, param2, param3, accessor));
        }

        [Fact]
        public void GetSequenceRunner_Sync()
        {
            SessionMockForRunner<IEnumerable<String>, IEnumerable<String>> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSequenceRunner<String>(new String[] { }, default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<IEnumerable<String>>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);

            Assert.Equal(runner, test_setup.Session.GetSequenceRunner<String>(RUNNER_NUMBER, null!));
            Assert.Null(test_setup.Session.GetSequenceRunner<String>(0, null!));
            Assert.Null(test_setup.Session.GetSequenceRunner<Int32>(RUNNER_NUMBER, null!));
        }

        [Fact]
        public void GetSequenceRunner_Async()
        {
            SessionMockForRunner<IAsyncEnumerable<String>, IEnumerable<String>> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSequenceRunner<String>(new String[] { }.AsAsyncEnumerable(), default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<IEnumerable<String>>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);

            Assert.Equal(runner, test_setup.Session.GetSequenceRunner<String>(RUNNER_NUMBER, null!));
            Assert.Null(test_setup.Session.GetSequenceRunner<String>(0, null!));
            Assert.Null(test_setup.Session.GetSequenceRunner<Int32>(RUNNER_NUMBER, null!));
        }

        [Fact]
        public void GetTimeSeriesRunner()
        {
            SessionMockForRunner<TimeSeriesParams<String>, IEnumerable<(DateTime, String)>> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateTimeSeriesRunner<String>(new TimeSeriesParams<String> { Gauge= () => DateTime.Now.ToString(), Interval=TimeSpan.FromSeconds(10) }, default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<IEnumerable<(DateTime, String)>>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);

            Assert.Equal(runner, test_setup.Session.GetTimeSeriesRunner<String>(RUNNER_NUMBER, null!));
            Assert.Null(test_setup.Session.GetTimeSeriesRunner<String>(0, null!));
            Assert.Null(test_setup.Session.GetTimeSeriesRunner<Int32>(RUNNER_NUMBER, null!));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        class SessionMockForRunner<TRequest, TResult>: IDisposable
        {
            Mock<IActiveSession> _mock;
            IRunner<TResult>? _dummyRunner = null;
            Expression<Func<IActiveSession, KeyedRunner<TResult>>> _createRunnerExpression =
                x => x.CreateRunner<TRequest, TResult>(It.IsAny<TRequest>(), It.IsAny<HttpContext>());
            Expression<Func<IActiveSession,Task?>> _trackExpression = x => x.TrackRunnerCleanup(RUNNER_NUMBER);
            Task? _trackCleanup = null;

            Mock<IDisposable> _accessor;
            SemaphoreSlim _pauser;
            Boolean _locked = false;

            public IActiveSession Session { get => _mock.Object; }
            public IDisposable GetAccessor() { _locked=true;  return _accessor.Object; } 

            public SessionMockForRunner()
            {
                _pauser=new SemaphoreSlim(0);
                _mock=new Mock<IActiveSession>();
                _mock.Setup(x => x.CreateRunner<It.IsAnyType, It.IsAnyType>(It.IsAny<It.IsAnyType>(), It.IsAny<HttpContext>()))
                    .Throws<InvalidOperationException>();
                _mock.Setup(_createRunnerExpression)
                    .Returns(new KeyedRunner<TResult> { Runner=_dummyRunner=(new Mock<IRunner<TResult>>()).Object!, RunnerNumber=RUNNER_NUMBER })
                    ;
                _mock.Setup(x => x.GetRunner<TResult>(It.IsAny<Int32>(), It.IsAny<HttpContext>())).Returns((IRunner<TResult>?)null);
                _mock.Setup(x=>x.GetRunner<TResult>(RUNNER_NUMBER, It.IsAny<HttpContext>())).Returns(_dummyRunner);
                _mock.Setup(x => x.TrackRunnerCleanup(It.IsAny<Int32>())).Returns((Task?)null);
                _mock.Setup(_trackExpression).Callback((int _) => { _locked=true;  }).Returns(_trackCleanup=Task.Run(() => { _pauser.Wait(); }));
                _accessor = new Mock<IDisposable>();
                _accessor.Setup(x => x.Dispose()).Callback(()=>_locked=false);
            }

            public void VerifyCreation()
            {
                _mock.Verify(_createRunnerExpression, Times.Once);
            }

            public void VerifyExclusive()
            {
                Assert.True(_locked);
                _mock.Verify(_trackExpression, Times.Once);
                Assert.NotNull(_trackCleanup);
                Assert.False(_trackCleanup.IsCompleted);
                _pauser.Release();
                Assert.True(_trackCleanup.Wait(5000));
                Task.Delay(100).GetAwaiter().GetResult();
                Assert.False(_locked);
            }

            public void Dispose()
            {
                if(_pauser.CurrentCount<=0) _pauser.Release();
                _pauser.Dispose();
            }
        }

    }

    public static class StdRunnerActiveSesionExtensionTestUtil 
    {
        public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T> Input)
        {
            foreach(var value in Input) {
                await Task.CompletedTask;
                yield return value;
            }
        }

    }

}
