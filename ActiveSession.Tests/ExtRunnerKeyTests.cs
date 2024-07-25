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
    }
}
