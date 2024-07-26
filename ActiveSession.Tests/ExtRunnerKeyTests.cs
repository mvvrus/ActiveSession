using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class ExtRunnerKeyTests
    {
        const String AS_ID = "382c74c3-721d-4f34-80e5-57657b6cbc27";
        [Fact]
        public void ToStringTest()
        {
            ExtRunnerKey value = new ExtRunnerKey(RunnerNumber: 42, Generation: 19, ActiveSessionId: AS_ID);
            Assert.Equal($"42-19-{AS_ID}", value.ToString());
        }

        [Fact]
        public void TryParseTest()
        {
            ExtRunnerKey sample = new ExtRunnerKey(RunnerNumber: 42, Generation: 19, ActiveSessionId:AS_ID), value;
            Assert.True(ExtRunnerKey.TryParse($"42-19-{AS_ID}", out value));
            Assert.Equal(sample, value);
            Assert.False(ExtRunnerKey.TryParse($" 42-19-{AS_ID}", out value));
            Assert.False(ExtRunnerKey.TryParse($"42-19 -{AS_ID}", out value));
            Assert.False(ExtRunnerKey.TryParse($"42:19-{AS_ID}", out value));
            Assert.False(ExtRunnerKey.TryParse("19-{AS_ID}", out value));
            Assert.False(ExtRunnerKey.TryParse("42-19-", out value));
        }

        [Fact]
        public void ImplicitConversionTest()
        {
            ExtRunnerKey sample = new ExtRunnerKey(RunnerNumber: 42, Generation: 19, ActiveSessionId: AS_ID), value;
            value=(42, AS_ID, 19);
            Assert.Equal(sample, value);

            Mock<IActiveSession> stub_session=new Mock<IActiveSession>();
            stub_session.SetupGet(s=>s.Id).Returns(AS_ID);
            stub_session.SetupGet(s => s.Generation).Returns(19);
            value= (stub_session.Object, 42);
            Assert.Equal(sample, value);
        }

        [Fact]
        public void IsForSessionTest()
        {
            ExtRunnerKey sample;
            Mock<IActiveSession> stub_session = new Mock<IActiveSession>();
            stub_session.SetupGet(s => s.Id).Returns(AS_ID);
            stub_session.SetupGet(s => s.Generation).Returns(19);

            sample= new ExtRunnerKey(RunnerNumber: 42, Generation: 19, ActiveSessionId: AS_ID);
            Assert.True(sample.IsForSession(stub_session.Object));

            sample= new ExtRunnerKey(RunnerNumber: 1, Generation: 19, ActiveSessionId: AS_ID);
            Assert.True(sample.IsForSession(stub_session.Object));

            sample= new ExtRunnerKey(RunnerNumber: 42, Generation: 1, ActiveSessionId: AS_ID);
            Assert.False(sample.IsForSession(stub_session.Object));

            sample= new ExtRunnerKey(RunnerNumber: 42, Generation: 19, ActiveSessionId: Guid.NewGuid().ToString());
            Assert.False(sample.IsForSession(stub_session.Object));
        }
    }
}
