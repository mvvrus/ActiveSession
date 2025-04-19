using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class SessionServiceTests
    {
        public interface IDummyService 
        {
            String Property { get; }
        };

        //Test group: SessionService class
        [Fact]
        public void SessionService()
        {
            //Arrange
            Boolean from_session = true;
            Mock<IDummyService> dummy_service = new Mock<IDummyService>();
            Mock<IServiceProvider> stub_sp= new Mock<IServiceProvider>();
            stub_sp.Setup(s => s.GetService(typeof(IDummyService))).Returns(dummy_service.Object);
            Mock<ActiveSessionRef> stub_sp_ref=new Mock<ActiveSessionRef>();
            stub_sp_ref.SetupGet(s=>s.IsFromSession).Returns(()=>from_session);
            stub_sp_ref.SetupGet(s => s.Services).Returns(stub_sp.Object);
            ISessionService<IDummyService> r1;
            ISessionService<ILoggerFactory> r2;
            //Test case: existing service from session
            //Act
            r1=new SessionService<IDummyService>(stub_sp_ref.Object);
            //Assess
            Assert.True(ReferenceEquals(dummy_service.Object,r1.Service));
            Assert.True(r1.IsFromSession);

            //Test case: non-existing service
            //Act
            r2=new SessionService<ILoggerFactory>(stub_sp_ref.Object);
            //Assess
            Assert.Null(r2.Service);

            //Test case: existing service from request context
            //Arrange more
            from_session=false;
            //Act
            r1=new SessionService<IDummyService>(stub_sp_ref.Object);
            //Assess
            Assert.False(r1.IsFromSession);
        }

        //Test group: ActiveSessionRef
        [Fact]
        public void ActiveSessionRef()
        {
            //Arrange
            Boolean avail = true;
            Mock<IServiceProvider> dummy_req_sp = new Mock<IServiceProvider>();
            Mock<IServiceProvider> dummy_session_sp = new Mock<IServiceProvider>();
            Mock<IActiveSession> stub_session = new Mock<IActiveSession>();
            Mock<ISessionServicesHelper> dummy_helper = stub_session.As<ISessionServicesHelper>();
            stub_session.SetupGet(s => s.SessionServices).Returns(dummy_session_sp.Object);
            stub_session.SetupGet(s => s.IsAvailable).Returns(() => avail);
            Mock<IActiveSessionFeature> stub_as_feature = new Mock<IActiveSessionFeature>();
            stub_as_feature.SetupGet(s => s.ActiveSession).Returns(stub_session.Object);
            Mock<IFeatureCollection> stub_features_col = new Mock<IFeatureCollection>();
            stub_features_col.Setup(s => s.Get<IActiveSessionFeature>()).Returns(stub_as_feature.Object);
            Mock<HttpContext> stub_context = new Mock<HttpContext>();
            stub_context.SetupGet(s => s.RequestServices).Returns(dummy_req_sp.Object);
            stub_context.SetupGet(s => s.Features).Returns(stub_features_col.Object);
            Mock<IHttpContextAccessor> stub_accessor = new Mock<IHttpContextAccessor>();
            stub_accessor.SetupGet(s=>s.HttpContext).Returns(stub_context.Object);
            //Test case: ActiveSession is available
            //Act
            ActiveSessionRef sp_ref = new ActiveSessionRef(stub_accessor.Object);
            //Assess
            Assert.True(sp_ref.IsFromSession);
            Assert.Same(dummy_session_sp.Object, sp_ref.Services);
            Assert.Same(stub_session.Object, sp_ref.ActiveSession);
            Assert.Same(dummy_helper.Object, sp_ref.SessionServiceHelper);

            //Test case: ActiveSession is not available
            //Arrange more
            avail=false;
            //Act
            sp_ref = new ActiveSessionRef(stub_accessor.Object);
            //Assess
            Assert.False(sp_ref.IsFromSession);
            Assert.True(ReferenceEquals(dummy_req_sp.Object, sp_ref.Services));
            Assert.Null(sp_ref.SessionServiceHelper);
        }

        //Test group: LockedSessionService
        [Fact]
        public void LockedSessionService()
        {
            LockedSessionService<ITest> tls;
            //Test case: session service (locked)
            Mock<ISessionServicesHelper> stub_asi= new Mock<ISessionServicesHelper>();
            stub_asi.Setup(s => s.ReleaseService(It.IsAny<Type>()));
            ITest test_service = new CTest();
            tls=new LockedSessionService<ITest>(stub_asi.Object, test_service);
            Assert.Same(test_service, tls.Service);
            Assert.True(tls.IsReallyLocked);
            tls.Dispose();
            stub_asi.Verify(s => s.ReleaseService(It.IsAny<Type>()), Times.Once);
            tls.Dispose();
            stub_asi.Verify(s => s.ReleaseService(It.IsAny<Type>()), Times.Once);
            //Test case: non-session service (unlocked)
            tls=new LockedSessionService<ITest>(null, test_service);
            Assert.Same(test_service, tls.Service);
            Assert.False(tls.IsReallyLocked);
            tls.Dispose(); //Assert: NullReferenceException isn't be thrown
        }

        Int32 TIMEOUT = 5000;
        Int32 SMALL_TIMEOUT = 200;

        //The function to setup SessionServiceLock test environment
        (
              Mock<IActiveSession>
            , Mock<ISessionServicesHelper>
            , Mock<IServiceProvider>
            , ITest
            , Mock<ActiveSessionRef>
        ) SetupSessionServiceLock(
              Func<Boolean> FromSessionFunc
            , Func<ISessionServicesHelper> LockerFunc
            , SemaphoreSlim Semaphore
        )
        {
            Mock<IActiveSession> dummy_session = new Mock<IActiveSession>();
            Mock<ISessionServicesHelper> stub_asi = dummy_session.As<ISessionServicesHelper>();
            Mock<IServiceProvider> stub_sp = new Mock<IServiceProvider>();
            ITest test_service = new CTest();
            stub_sp.Setup(s => s.GetService(It.IsAny<Type>())).Returns(null);
            stub_sp.Setup(s => s.GetService(typeof(ITest))).Returns(test_service);

            Mock<ActiveSessionRef> stub_sessionref = new Mock<ActiveSessionRef>();
            stub_sessionref.SetupGet(s => s.IsFromSession).Returns(FromSessionFunc);
            stub_sessionref.SetupGet(s => s.Services).Returns(stub_sp.Object);
            stub_sessionref.SetupGet(s => s.ActiveSession).Returns(dummy_session.Object);
            stub_sessionref.SetupGet(s => s.SessionServiceHelper).Returns(LockerFunc);
            Task<Boolean> wait_task = Task.FromException<Boolean>(new InvalidOperationException("Return value uninitialized."));
            stub_asi.Setup(s => s.WaitForServiceAsync(It.IsAny<Type>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Callback((Type _, TimeSpan d, CancellationToken t) => { wait_task=MakeWaitTask(d, t); })
                .Returns(() => wait_task);
            stub_asi.Setup(s => s.ReleaseService(It.IsAny<Type>())).Callback(() => Semaphore.Release());

            return (
                  dummy_session
                , stub_asi
                , stub_sp
                , test_service
                , stub_sessionref
            );

            async Task<Boolean> MakeWaitTask(TimeSpan d, CancellationToken t)
            {
                Boolean wait_result = await Semaphore.WaitAsync(d, t);
                if(wait_result)
#pragma warning disable VSTHRD103 // Call async methods when in an async method
                    lock(Semaphore) if(Semaphore.Wait(0)) Semaphore.Release();  //This is a check if the semaphore has been disposed
#pragma warning restore VSTHRD103 // Call async methods when in an async method
                return wait_result;
            }
        }

        //Test group: SessionServiceLock single call
        [Fact]
        public void SessionServiceLock_Single()
        {
            Boolean from_session = false;
            ISessionServicesHelper asi = null!;

            SessionServiceLock<ITest> ssl;
            Task<ILockedSessionService<ITest>?> tlss;
            ILockedSessionService<ITest>? lss;

            SemaphoreSlim semaphore = new SemaphoreSlim(1, 2);
            try {
                (
                      Mock<IActiveSession> dummy_session
                    , Mock<ISessionServicesHelper> stub_asi
                    , Mock<IServiceProvider> stub_sp
                    , ITest test_service
                    , Mock<ActiveSessionRef> stub_sessionref
                ) = SetupSessionServiceLock(
                      () => from_session
                    , () => asi
                    , semaphore
                );

                //Test case: normal operation (from session DI container, service is registered)
                from_session = true;
                asi = stub_asi.Object;
                ssl= new SessionServiceLock<ITest>(stub_sessionref.Object);
                tlss=ssl.AcquireAsync(Timeout.InfiniteTimeSpan, default);
                Assert.True(tlss.Wait(TIMEOUT));
                using(lss=tlss.Result) {
                    Assert.NotNull(lss);
                    Assert.True(lss.IsReallyLocked);
                    Assert.Same(test_service, lss.Service);
                }

                //Test case: service is not registered
                SessionServiceLock<ITestBis> ssl_bis = new SessionServiceLock<ITestBis>(stub_sessionref.Object);
                Task<ILockedSessionService<ITestBis>?> tlss_bis = ssl_bis.AcquireAsync(Timeout.InfiniteTimeSpan, default);
                Assert.True(tlss_bis.Wait(TIMEOUT));
                using(ILockedSessionService<ITestBis>? lss_bis = tlss_bis.Result) {
                    Assert.NotNull(lss_bis);
                    Assert.True(lss_bis.IsReallyLocked);
                    Assert.Null(lss_bis.Service);
                }

                Assert.True(semaphore.Wait(TIMEOUT));
                try {
                    //Test case: locking timed out
                    ssl= new SessionServiceLock<ITest>(stub_sessionref.Object);
                    tlss=ssl.AcquireAsync(TimeSpan.FromMilliseconds(SMALL_TIMEOUT), default);
                    Assert.True(tlss.Wait(TIMEOUT));
                    Assert.Null(tlss.Result);

                    //Test case: locking canceled
                    ssl= new SessionServiceLock<ITest>(stub_sessionref.Object);
                    using(CancellationTokenSource cts = new CancellationTokenSource(SMALL_TIMEOUT)) {
                        tlss=ssl.AcquireAsync(Timeout.InfiniteTimeSpan, cts.Token);
                        AggregateException ae = Assert.Throws<AggregateException>(() => tlss.Wait(TIMEOUT));
                        Assert.Single(ae.InnerExceptions);
                        Assert.IsType<TaskCanceledException>(ae.InnerExceptions[0]);
                    }

                }
                finally {
                    semaphore.Release();
                }

                //Test case: from request DI container
                from_session = false;
                asi=null!;
                ssl= new SessionServiceLock<ITest>(stub_sessionref.Object);
                tlss=ssl.AcquireAsync(Timeout.InfiniteTimeSpan, default);
                Assert.True(tlss.Wait(TIMEOUT));
                using(lss=tlss.Result) {
                    Assert.NotNull(lss);
                    Assert.False(lss.IsReallyLocked);
                    Assert.Same(test_service, lss.Service);
                }

                //Test case: from session DI container, lack of lock support
                from_session = true;
                asi=null!;
                ssl= new SessionServiceLock<ITest>(stub_sessionref.Object);
                tlss=ssl.AcquireAsync(Timeout.InfiniteTimeSpan, default);
                AggregateException e = Assert.Throws<AggregateException>(() => tlss.Wait(TIMEOUT));
                Assert.Single(e.InnerExceptions);
                Assert.IsType<NotImplementedException>(e.InnerExceptions[0]);

                //Test case: an outstanding lock exists while disposing
                from_session = true;
                asi = stub_asi.Object;
                Assert.True(semaphore.Wait(TIMEOUT));
                ssl= new SessionServiceLock<ITest>(stub_sessionref.Object);
                tlss=ssl.AcquireAsync(Timeout.InfiniteTimeSpan, default);
                Assert.False(tlss.Wait(SMALL_TIMEOUT));
            }
            finally {
                lock(semaphore) {
                    semaphore.Release();
                    semaphore.Dispose();
                }
            }
            AggregateException oe = Assert.Throws<AggregateException>(() => tlss.Wait(TIMEOUT));
            Assert.Single(oe.InnerExceptions);
            Assert.IsType<ObjectDisposedException>(oe.InnerExceptions[0]);
        }

        //Test group: SessionServiceLock sequence of two calls using the same service
        [Fact]
        public async Task SessionServiceLock_Sequence()
        {
            Boolean from_session = false;
            ISessionServicesHelper asi = null!;


            SemaphoreSlim semaphore = new SemaphoreSlim(1, 2);
            try {
                (
                      Mock<IActiveSession> dummy_session
                    , Mock<ISessionServicesHelper> stub_asi
                    , Mock<IServiceProvider> stub_sp
                    , ITest test_service
                    , Mock<ActiveSessionRef> stub_sessionref
                ) = SetupSessionServiceLock(
                      () => from_session
                    , () => asi
                    , semaphore
                );

                asi=stub_asi.Object;

                using(SemaphoreSlim pauser=new SemaphoreSlim(0,1)) {
                    Boolean flag1, flag2;
                    Task task1, task2, delay;
                    SessionServiceLock<ITest> ssl;

                    //Test case: from session DI container
                    from_session = true;
                    Reset();
                    ssl= new SessionServiceLock<ITest>(stub_sessionref.Object);

                    task1 = Job1();
                    task2 = Job2();
                    delay = Task.Delay(SMALL_TIMEOUT);
                    Assert.Same(delay, await Task.WhenAny(new Task[] { task1, task2, delay }));
                    Assert.True(flag1);
                    Assert.False(flag2);

                    pauser.Release();
                    delay = Task.Delay(TIMEOUT);
                    Assert.NotSame(delay, await Task.WhenAny(new Task[] { Task.WhenAll(new Task[] { task1, task2 }), delay }));
                    Assert.True(flag2);

                    //Test case: from request DI container
                    from_session = false;
                    Reset();
                    ssl= new SessionServiceLock<ITest>(stub_sessionref.Object);

                    task1 = Job1();
                    task2 = Job2();
                    delay = Task.Delay(SMALL_TIMEOUT);
                    Assert.NotSame(delay, await Task.WhenAny(new Task[] { task1, task2, delay }));
                    Assert.False(task1.IsCompleted);
                    Assert.True(task2.IsCompletedSuccessfully);
                    Assert.True(flag1);
                    Assert.True(flag2);

                    pauser.Release();
                    delay = Task.Delay(TIMEOUT);
                    Assert.NotSame(delay, await Task.WhenAny(new Task[] { Task.WhenAll(new Task[] { task1, task2 }), delay }));

                    void Reset()
                    {
                        flag1=false;
                        flag2=false;
                        if(pauser.CurrentCount>0) pauser.Wait();
                    }

                    async Task Job1()
                    {
                        using(ILockedSessionService<ITest>? ls = await ssl.AcquireAsync(Timeout.InfiniteTimeSpan, default)) {
                            flag1=true;
                            await pauser.WaitAsync();
                        }
                    }

                    async Task Job2()
                    {
                        using(ILockedSessionService<ITest>? ls = await ssl.AcquireAsync(Timeout.InfiniteTimeSpan, default)) {
                            flag2=true;
                        }
                    }
                }
            }
            finally {
                lock(semaphore) {
                    semaphore.Release();
                    semaphore.Dispose();
                }
            }
        }

        interface ITest { }
        class CTest : ITest { };
        interface ITestBis { }
        class CTestBis : ITestBis { };
    }
}
