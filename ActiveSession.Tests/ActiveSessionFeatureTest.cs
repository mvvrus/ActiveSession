using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MVVrus.AspNetCore.ActiveSession.Internal;


namespace ActiveSession.Tests
{
    public class ActiveSessionFeatureTest
    {
        const  String TEST_TRACE_IDENTIFIER="TEST_TRACE_IDENTIFIER";

        [Fact]
        public void CreateActiveSessionFeature() 
        {
            ConstructorTestSetup test_setup = new ConstructorTestSetup();

            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.DummyStore.Object, test_setup.MockSession.Object, null, TEST_TRACE_IDENTIFIER);

            Assert.Equal(test_setup.DummyStore.Object, feature.Store);
            Assert.Equal(test_setup.MockSession.Object, feature.Session);
            Assert.Null(feature.Logger);
            Assert.Equal(TEST_TRACE_IDENTIFIER, feature.TraceIdentifier);
            Assert.Equal(ActiveSessionFeature.DummySession, feature._ActiveSession);
            Assert.False(feature.IsLoaded);
        }

        [Fact]
        public void CommitAsync()
        {
            CommitAsyncTestSetup test_setup = new CommitAsyncTestSetup();
            ActiveSessionFeature feature = new ActiveSessionFeature(test_setup.DummyStore.Object, test_setup.MockSession.Object, null, TEST_TRACE_IDENTIFIER);
//TODO Load()?
            feature.CommitAsync().GetAwaiter().GetResult();

            test_setup.MockSession.Verify(test_setup.SessionCommitAsyncExpression, Times.Once);
        }


        class ConstructorTestSetup
        {
            public Mock<IActiveSessionStore> DummyStore;
            public Mock<ISession> MockSession;

            public ConstructorTestSetup()
            {
                DummyStore=new Mock<IActiveSessionStore>();
                MockSession=new Mock<ISession>();
            }
        }

        class CommitAsyncTestSetup: ConstructorTestSetup
        {
            public Expression<Action<ISession>> SessionCommitAsyncExpression = s => s.CommitAsync(It.IsAny<CancellationToken>());
            public CommitAsyncTestSetup() : base() 
            {
                MockSession.Setup(SessionCommitAsyncExpression);
            }
            
        }
    }

}
