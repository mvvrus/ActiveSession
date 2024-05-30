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

        [Fact]
        public void CreateSequenceRunner_Sync()
        {
            SessionMockForRunner<IEnumerable<String>, IEnumerable<String>> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSequenceRunner<String>(new String[] { }, default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<IEnumerable<String>>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        [Fact]
        public void CreateSequenceRunner_SyncParam()
        {
            SessionMockForRunner<EnumAdapterParams<String>, IEnumerable<String>> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSequenceRunner<String>(new EnumAdapterParams<String> { Source = new String[] { } }, default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<IEnumerable<String>>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        [Fact]
        public void CreateSequenceRunner_Async()
        {
            SessionMockForRunner<IAsyncEnumerable<String>, IEnumerable<String>> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSequenceRunner<String>(new String[] { }.AsAsyncEnumerable(), default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<IEnumerable<String>>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        [Fact]
        public void CreateSequenceRunner_AsyncParam()
        {
            SessionMockForRunner<AsyncEnumAdapterParams<String>, IEnumerable<String>> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSequenceRunner<String>(new AsyncEnumAdapterParams<String> { Source = new String[] { }.AsAsyncEnumerable() }, default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<IEnumerable<String>>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        [Fact]
        public void CreateTimeSeriesRunner_Param()
        {
            SessionMockForRunner<TimeSeriesParams<String>, IEnumerable<(DateTime,String)>> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateTimeSeriesRunner<String>(new TimeSeriesParams<String> {Gauge= () =>DateTime.Now.ToString(), Interval=TimeSpan.FromSeconds(10)} , default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<IEnumerable<(DateTime,String)>>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        [Fact]
        public void CreateTimeSeriesRunner_Infinite()
        {
            SessionMockForRunner<(Func<String>,TimeSpan), IEnumerable<(DateTime, String)>> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateTimeSeriesRunner<String>(() => DateTime.Now.ToString(), TimeSpan.FromSeconds(10) , default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<IEnumerable<(DateTime, String)>>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        [Fact]
        public void CreateTimeSeriesRunner_Finite()
        {
            SessionMockForRunner<(Func<String>, TimeSpan,Int32), IEnumerable<(DateTime, String)>> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateTimeSeriesRunner<String>(() => DateTime.Now.ToString(), TimeSpan.FromSeconds(10), 6, default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<IEnumerable<(DateTime, String)>>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        [Fact]
        public void CreateSessionProcessRunner_BodyWithResult()
        {
            SessionMockForRunner<Func<Action<String, Int32?>, CancellationToken, String>, String> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSessionProcessRunner<String>(
                (_, _)=> "Hello!",
                default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<String>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        [Fact]
        public void CreateSessionProcessRunner_BodyWithoutResult()
        {
            SessionMockForRunner<Action<Action<String, Int32?>, CancellationToken>, String> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSessionProcessRunner<String>(
                (_, _) => { },
                default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<String>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        [Fact]
        public void CreateSessionProcessRunner_CreatorWithResult()
        {
            SessionMockForRunner<Func<Action<String, Int32?>, CancellationToken, Task<String>>, String> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSessionProcessRunner<String>(
                async (_, _) => "Hello!",
                default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<String>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        [Fact]
        public void CreateSessionProcessRunner_CreatorWithoutResult()
        {
            SessionMockForRunner<Func<Action<String, Int32?>, CancellationToken, Task>, String> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSessionProcessRunner<String>(
                async (_, _) => { },
                default!);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<String>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        //Func<Action<String, Int32?>, CancellationToken, String>, CancellationTokenSource, Boolean
        //Action<Action<String, Int32?>, CancellationToken>, CancellationTokenSource, Boolean
        //Func<Action<String, Int32?>, CancellationToken, Task<String>>, CancellationTokenSource, Boolean
        //Func<Action<String, Int32?>, CancellationToken, Task>,  CancellationTokenSource, Boolean
        [Fact]
        public void CreateSessionProcessRunner_BodyWithResultExtCts()
        {
            SessionMockForRunner<(Func<Action<String, Int32?>, CancellationToken, String>, CancellationTokenSource, Boolean), String> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSessionProcessRunner<String>(
                (_, _) => "Hello!",
                default!, (CancellationTokenSource)null!, false);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<String>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        [Fact]
        public void CreateSessionProcessRunner_BodyWithoutResulExtCtst()
        {
            SessionMockForRunner<(Action<Action<String, Int32?>, CancellationToken>, CancellationTokenSource, Boolean), String> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSessionProcessRunner<String>(
                (_, _) => { },
                default!, (CancellationTokenSource)null!, false);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<String>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        [Fact]
        public void CreateSessionProcessRunner_CreatorWithResultExtCts()
        {
            SessionMockForRunner<(Func<Action<String, Int32?>, CancellationToken, Task<String>>, CancellationTokenSource, Boolean), String> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSessionProcessRunner<String>(
                async (_, _) => "Hello!",
                default!, (CancellationTokenSource)null!, false);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<String>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
        }

        [Fact]
        public void CreateSessionProcessRunner_CreatorWithoutResultExtCts()
        {
            SessionMockForRunner<(Func<Action<String, Int32?>, CancellationToken, Task>, CancellationTokenSource, Boolean), String> test_setup = new();
            (var runner, Int32 number) = test_setup.Session.CreateSessionProcessRunner<String>(
                async (_, _) => { },
                default!, (CancellationTokenSource)null!, false);
            test_setup.VerifyCreation();
            Assert.NotNull(runner);
            Assert.IsAssignableFrom<IRunner<String>>(runner);
            Assert.Equal(RUNNER_NUMBER, number);
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
        class SessionMockForRunner<TRequest, TResult>
        {
            Mock<IActiveSession> _mock;
            IRunner<TResult>? _dummyRunner = null;
            Expression<Func<IActiveSession, KeyedRunner<TResult>>> _createRunnerExpression =
                x => x.CreateRunner<TRequest, TResult>(It.IsAny<TRequest>(), It.IsAny<HttpContext>());

            public IActiveSession Session { get => _mock.Object; }

            public SessionMockForRunner()
            {
                _mock=new Mock<IActiveSession>();
                //TODO mock CreateRunner<TRequest,TResult> method
                _mock.Setup(x => x.CreateRunner<It.IsAnyType, It.IsAnyType>(It.IsAny<It.IsAnyType>(), It.IsAny<HttpContext>()))
                    .Throws<InvalidOperationException>();
                _mock.Setup(_createRunnerExpression)
                    .Returns(new KeyedRunner<TResult> { Runner=_dummyRunner=(new Mock<IRunner<TResult>>()).Object!, RunnerNumber=RUNNER_NUMBER })
                    ;
                _mock.Setup(x=>x.GetRunner<TResult>(RUNNER_NUMBER, It.IsAny<HttpContext>())).Returns(_dummyRunner);
            }

            public void VerifyCreation()
            {
                _mock.Verify(_createRunnerExpression, Times.Once);
            }

        }

    }

    public static class StdRunnerActiveSesionExtensionTestUtil 
    {
        public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T> Input)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            foreach(var value in Input) {
                yield return value;
            }
        }

    }

}
