using Microsoft.Extensions.DependencyInjection;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class ActiveSessionGroupTests
    {
        const String TEST_ID = "TEST_GROUP_ID";

        //Test case: creation and Id, IsAvailable and SessionServices properties
        [Fact]
        public void CreationTest()
        {
            ActiveSessionGroup group,group2;
            TestSetup ts = new TestSetup();
            IServiceProvider sp, sp2;
            TestSetup.Indicator? ind=null, ind2=null;

            using(group=new ActiveSessionGroup(TEST_ID, ts.RootServiceProviderMock.Object)) {
                sp = group.SessionServices;
                ind = Assert.IsType<TestSetup.Indicator>(sp.GetService(typeof(TestSetup.Indicator)));
                Assert.NotNull(ind);
                Assert.False(ind.Disposed);

                Assert.Equal(TEST_ID, group.Id);
                Assert.True(group.IsAvailable);
                using(group2=new ActiveSessionGroup(TEST_ID+"2", ts.RootServiceProviderMock.Object)) {
                    sp2= group2.SessionServices;
                    ind2 = Assert.IsType<TestSetup.Indicator>(sp2.GetService(typeof(TestSetup.Indicator)));
                    Assert.NotSame(sp, sp2);
                    Assert.NotSame(ind, ind2);
                    Assert.NotNull(ind2);
                    Assert.False(ind2.Disposed);
                }
                Assert.True(ind2.Disposed);
                Assert.False(ind.Disposed);
            }
            Assert.True(ind.Disposed);
        }

        //Test group: CompletionToken property.
        [Fact]
        public async Task CompletionTokenTest()
        {
            const Int32 SMALL_TIMEOUT = 200;

            ActiveSessionGroup group;
            TaskCompletionSource cancel_tcs=null!;
            SemaphoreSlim start_semaphore = new SemaphoreSlim(1,1);

            TestSetup ts = new TestSetup();

            //Test case: get the CompletionToken value before Dispose and await for it to be canceled (via Dispose)
            group=new ActiveSessionGroup(TEST_ID, ts.RootServiceProviderMock.Object);
            Assert.False(group.CompletionToken.IsCancellationRequested);
            Assert.True(group.CompletionToken.CanBeCanceled);

            Task task_to_wait;
            task_to_wait=start_semaphore.WaitAsync();
            Assert.Same(task_to_wait, await Task.WhenAny(task_to_wait, Task.Delay(SMALL_TIMEOUT)));
            cancel_tcs = new TaskCompletionSource();
            using(group.CompletionToken.Register(TokenCallback)) {
                Func<Task> delayed_dispose = async () => { await start_semaphore.WaitAsync(); group.Dispose(); };
                Assert.NotSame(cancel_tcs.Task, await Task.WhenAny(cancel_tcs.Task, Task.Delay(SMALL_TIMEOUT)));
                start_semaphore.Release();
                Task curwait = delayed_dispose();
                Assert.Same(curwait, await Task.WhenAny(curwait, Task.Delay(SMALL_TIMEOUT)));
                Assert.Same(cancel_tcs.Task, await Task.WhenAny(cancel_tcs.Task, Task.Delay(SMALL_TIMEOUT)));
            }

            //Test case: get the CompletionToken value then it is canceled already (via Dispose) and and work with it as if it was not disposed 
            cancel_tcs = new TaskCompletionSource();
            group=new ActiveSessionGroup(TEST_ID, ts.RootServiceProviderMock.Object);

            group.Dispose();
            Assert.True(group.CompletionToken.IsCancellationRequested);
            using(group.CompletionToken.Register(TokenCallback)) {
                Assert.Same(cancel_tcs.Task, await Task.WhenAny(cancel_tcs.Task, Task.Delay(SMALL_TIMEOUT)));
            }

            void TokenCallback()
            {
                cancel_tcs.SetResult();
            }
        }

        //Test group: Properties property
        [Fact]
        public void Properties()
        {
            TestSetup ts;
            ActiveSessionGroup  group;

            const String KEY1 = "key1", KEY2 = "key2", KEY3 = "key3";
            Object value1 = new Object(), value2 = new Object();

            //Test case: Set properties
            ts=new TestSetup();
            group=new ActiveSessionGroup(TEST_ID, ts.RootServiceProviderMock.Object);
            group.Properties[KEY1]=value1;
            group.Properties[KEY2]=value2;
            Assert.Equal(2, group.Properties.Count);
            //Test case: retrieve properties
            Assert.True(group.Properties.ContainsKey(KEY1));
            Assert.True(group.Properties.ContainsKey(KEY2));
            Assert.False(group.Properties.ContainsKey(KEY3));
            Assert.Same(value1, group.Properties[KEY1]);
            Assert.Same(value2, group.Properties[KEY2]);
            Assert.Throws<KeyNotFoundException>(() => group.Properties[KEY3]);
            //Test case: access properties after dispose
            group.Dispose();
            Assert.Same(value1, group.Properties[KEY1]);
        }



        class TestSetup
        {
            Func<IServiceScope> _scopeFactoryDelegate;
            Mock<IServiceScopeFactory> _scopeFactoryMock;
            public Mock<IServiceProvider> RootServiceProviderMock;
            

            IServiceScope MakeScope()
            {
                Mock<IServiceScope> mock_scope = new Mock<IServiceScope>(MockBehavior.Strict);
                Mock<IServiceProvider> mock_provider=new Mock<IServiceProvider>(MockBehavior.Strict);
                Indicator indicator = new Indicator();
                mock_provider.Setup(s => s.GetService(It.IsAny<Type>())).Returns(null);
                mock_provider.Setup(s => s.GetService(typeof(Indicator))).Returns(indicator);
                mock_scope.SetupGet(s => s.ServiceProvider).Returns(mock_provider.Object);
                mock_scope.Setup(s => s.Dispose()).Callback(() => { indicator.Disposed=true; } );
                return mock_scope.Object;
            }

            public TestSetup()
            {
                _scopeFactoryDelegate = MakeScope;
                _scopeFactoryMock = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
                _scopeFactoryMock.Setup(s => s.CreateScope()).Returns(() => _scopeFactoryDelegate());
                RootServiceProviderMock = new Mock<IServiceProvider>(MockBehavior.Strict);
                RootServiceProviderMock.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(()=>_scopeFactoryMock.Object);
            }

            public class Indicator
            {
                public Boolean Disposed { get; set; } = false;
            }
        }
    }
}
