using Microsoft.Extensions.DependencyInjection;
using MVVrus.AspNetCore.ActiveSession;
using MVVrus.AspNetCore.ActiveSession.Internal;
using System.Collections.ObjectModel;

namespace ActiveSession.Tests
{
    public class ActiveSessionServiceCollectionExtensionsTests
    {
        public record Request1
        {
            public String Arg { get; set; } = "";
        }

        public class Result1
        {
            public String Value { get; init; }
            public Result1(String Value)
            {
                this.Value=Value;
            }
        }

        public class SpyRunner1 : SpyRunnerBase<Result1>
        {
            public Request1 Request { get; init; }
            public SpyRunner1(Request1 Request)
            {
                this.Request=Request;
            }
        }


        [Fact]
        public void DelegateFactoryAddition()
        {
            IServiceCollection services = new ServiceCollection();
            Func<Request1, IActiveSessionRunner<Result1>> factory = x => new SpyRunner1(x);
            services.AddActiveSessions(factory);
        }
    }
}
