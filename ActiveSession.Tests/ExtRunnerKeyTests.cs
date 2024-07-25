using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class ExtRunnerKeyTests
    {
        [Fact]
        public void ToStringTest()
        {
            ExtRunnerKey value = new ExtRunnerKey(RunnerNumber: 42, Generation: 9);
            Assert.Equal("9-42", value.ToString());
        }

        [Fact]
        public void TryParseTest()
        {
            ExtRunnerKey sample = new ExtRunnerKey(RunnerNumber: 42, Generation: 19), value;
            Assert.True(ExtRunnerKey.TryParse("19-42", out value));
            Assert.False(ExtRunnerKey.TryParse(" 19-42", out value));
            Assert.False(ExtRunnerKey.TryParse("19-42 ", out value));
            Assert.False(ExtRunnerKey.TryParse("19:42", out value));
            Assert.False(ExtRunnerKey.TryParse("19-", out value));
            Assert.False(ExtRunnerKey.TryParse("-42", out value));
            Assert.False(ExtRunnerKey.TryParse("19-42-1", out value));
        }
    }
}
