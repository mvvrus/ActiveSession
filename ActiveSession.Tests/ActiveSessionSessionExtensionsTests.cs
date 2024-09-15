using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class ActiveSessionSessionExtensionsTests
    {
        const Int32 RUNNER_NUMBER = 42;

        [Fact]
        public void CreateRunnerWithExclusiveService()
        {
            Mock<IActiveSession> _mock;
            IRunner<String>? _dummyRunner = null;
            Expression<Func<IActiveSession, KeyedRunner<String>>> _createRunnerExpression =
                x => x.CreateRunner<Int32, String>(It.IsAny<Int32>(), It.IsAny<HttpContext>());
            Expression<Func<IActiveSession, Task?>> _trackExpression = x => x.TrackRunnerCleanup(RUNNER_NUMBER);
            Task? _trackCleanup = null;

            Mock<IDisposable> _accessor;
            SemaphoreSlim _pauser=null!;
            Boolean _locked = false;

            
            _mock=new Mock<IActiveSession>();
            _mock.Setup(x => x.CreateRunner<It.IsAnyType, It.IsAnyType>(It.IsAny<It.IsAnyType>(), It.IsAny<HttpContext>()))
                .Throws<InvalidOperationException>();
            _mock.Setup(_createRunnerExpression)
                .Returns(new KeyedRunner<String> { Runner=_dummyRunner=(new Mock<IRunner<String>>()).Object!, RunnerNumber=RUNNER_NUMBER })
                ;
            _mock.Setup(x => x.GetRunner<String>(It.IsAny<Int32>(), It.IsAny<HttpContext>())).Returns((IRunner<String>?)null);
            _mock.Setup(x => x.GetRunner<String>(RUNNER_NUMBER, It.IsAny<HttpContext>())).Returns(_dummyRunner);
            _mock.Setup(x => x.TrackRunnerCleanup(It.IsAny<Int32>())).Returns((Task?)null);
            _mock.Setup(_trackExpression).Callback((int _) => { _locked=true; }).Returns(_trackCleanup=Task.Run(() => { _pauser.Wait(); }));
            _accessor = new Mock<IDisposable>();
            _accessor.Setup(x => x.Dispose()).Callback(() => _locked=false);

            using(_pauser=new SemaphoreSlim(0)) {
                (IRunner<String> runner, Int32 number) =
                    ActiveSessionExtensions.CreateRunnerWithExclusiveService<Int32,String>(_mock.Object, 1, default!, GetAccessor());
                Assert.True(_locked);
                _mock.Verify(_trackExpression, Times.Once);
                Assert.NotNull(_trackCleanup);
                Assert.False(_trackCleanup.IsCompleted);
                _pauser.Release();
                Assert.True(_trackCleanup.Wait(5000));
                Task.Delay(100).GetAwaiter().GetResult();
                Assert.False(_locked);
            }

            IDisposable GetAccessor() { _locked=true; return _accessor.Object; }
        }
    }
}
