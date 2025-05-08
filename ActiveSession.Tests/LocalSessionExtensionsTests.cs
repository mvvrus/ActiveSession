using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class LocalSessionExtensionsTests
    {
        //Test case: TakeOwnership happy path
        [Fact]
        public void TakeOwnershipNormal()
        {
            using (TestSetup ts=new TestSetup() { IsAvailable=true }) {
                TestDisposable td = new TestDisposable();
                ts.LocalSession.TakeOwnership(td);
                CancellationToken token = ts.LocalSession.CompletionToken;
                ts.Cancel();
                Assert.True(token.IsCancellationRequested);
                Assert.True(td.Disposed);
            }
        }

        //Test case: TakeOwnership premature cleanup of the associated object
        [Fact]
        public void TakeOwnershipPrematureCleanup()
        {
            using(TestSetup ts = new TestSetup() { IsAvailable=true }) {
                TestDisposable td = new TestDisposable();
                IDisposable reg=ts.LocalSession.TakeOwnership(td);
                CancellationToken token = ts.LocalSession.CompletionToken;
                reg.Dispose();
                ts.Cancel();
                Assert.True(token.IsCancellationRequested);
                Assert.True(td.Disposed);
            }
        }

        //Test case: TakeOwnership on unavailable LocalSession
        [Fact]
        public void TakeOwnershipUnavailable()
        {
            using(TestSetup ts = new TestSetup() { IsAvailable=false }) {
                TestDisposable td = new TestDisposable();
                Assert.Throws<InvalidOperationException>(()=>ts.LocalSession.TakeOwnership(td));
            }
        }

        //Test case: TakeOwnership happy path and then cleanup
        [Fact]
        public void TakeOwnershipLateCleanup()
        {
            using(TestSetup ts = new TestSetup() { IsAvailable=true }) {
                TestDisposable td = new TestDisposable();
                IDisposable reg = ts.LocalSession.TakeOwnership(td);
                CancellationToken token = ts.LocalSession.CompletionToken;
                ts.Cancel();
                reg.Dispose();
                Assert.True(token.IsCancellationRequested);
                Assert.True(td.Disposed);
            }
        }

        class TestDisposable : IDisposable
        {
            public Boolean Disposed { get; private set; }
            public void Dispose()
            {
                if(Disposed) throw new ObjectDisposedException(GetType().FullName);
                Disposed=true;
            }
        }

        class TestSetup : IDisposable
        {
            Mock<ILocalSession> _localSessionMock;
            CancellationTokenSource _cts;

            public ILocalSession LocalSession => _localSessionMock.Object;
            public Boolean IsAvailable { get; set; }

            public TestSetup()
            {
                _localSessionMock = new Mock<ILocalSession>(MockBehavior.Strict);
                _localSessionMock.SetupGet(s => s.IsAvailable).Returns(() => IsAvailable);
                _cts = new CancellationTokenSource();
                _localSessionMock.SetupGet(s => s.CompletionToken).Returns(() => _cts.Token);
            }

            public void Cancel()
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts=new CancellationTokenSource();
            }

            public void Dispose()
            {
                _cts.Dispose();
            }
        }
    }
}
