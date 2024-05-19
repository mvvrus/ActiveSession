using Microsoft.Extensions.DependencyInjection;
using MVVrus.AspNetCore.ActiveSession.StdRunner;
using static ActiveSession.Tests.ServiceCollectionExtensionTestUtils;

namespace ActiveSession.Tests
{
    public class StdRunnerServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddEnumRunner_NoConfig()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddEnumAdapter<Int32>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, new Type[] { typeof(IEnumerable<Int32>), typeof(EnumAdapterParams<Int32>) }, 
                new Type[] { typeof(IEnumerable<Int32>) }, typeof(EnumAdapterRunner<Int32>), ReqParams:new Int32[] {2,2 });
        }

        [Fact]
        public void AddEnumRunner_Config()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddEnumAdapter<Int32>(o => o.HostId="unknown");

            CheckInfrastructure(services, true);
            CheckTypeFactories(services, new Type[] { typeof(IEnumerable<Int32>), typeof(EnumAdapterParams<Int32>) },
                new Type[] { typeof(IEnumerable<Int32>) }, typeof(EnumAdapterRunner<Int32>), ReqParams: new Int32[] { 2, 2 });
        }

        [Fact]
        public void AddAsyncEnumRunner_NoConfig()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddAsyncEnumAdapter<Int32>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, new Type[] { typeof(IAsyncEnumerable<Int32>), typeof(AsyncEnumAdapterParams<Int32>) },
                new Type[] { typeof(IEnumerable<Int32>) }, typeof(AsyncEnumAdapterRunner<Int32>), ReqParams: new Int32[] { 2, 2 });
        }

        [Fact]
        public void AddAsyncEnumRunner_Config()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddAsyncEnumAdapter<Int32>(o => o.HostId="unknown");

            CheckInfrastructure(services, true);
            CheckTypeFactories(services, new Type[] { typeof(IAsyncEnumerable<Int32>), typeof(AsyncEnumAdapterParams<Int32>) },
                new Type[] { typeof(IEnumerable<Int32>) }, typeof(AsyncEnumAdapterRunner<Int32>), ReqParams: new Int32[] { 2, 2 });
        }

        [Fact]
        public void AddTimeSeriesRunner_NoConfig()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddTimeSeriesRunner<Int32>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, new Type[] { typeof(ValueTuple<Func<Int32>, TimeSpan>), typeof(ValueTuple<Func<Int32>, TimeSpan, Int32>) },
                new Type[] { typeof(IEnumerable<ValueTuple<DateTime,Int32>>) }, typeof(TimeSeriesRunner<Int32>), ReqParams: new Int32[] { 2, 2 });
        }

        [Fact]
        public void AddTimeSeriesRunner_Config()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddTimeSeriesRunner<Int32>(o => o.HostId="unknown");

            CheckInfrastructure(services, true);
            CheckTypeFactories(services, new Type[] { typeof(ValueTuple<Func<Int32>, TimeSpan>), typeof(ValueTuple<Func<Int32>, TimeSpan, Int32>) },
                new Type[] { typeof(IEnumerable<ValueTuple<DateTime, Int32>>) }, typeof(TimeSeriesRunner<Int32>), ReqParams: new Int32[] { 2, 2 });
        }

        [Fact]
        public void AddSessionProcessRunner_NoConfig()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddSessionProcessRunner<Int32>();

            CheckInfrastructure(services, false);
            CheckTypeFactories(services, new Type[] { typeof(Func<Action<Int32, Int32?>, CancellationToken, Int32>),
                    typeof(Action<Action<Int32, Int32?>, CancellationToken>) },
                new Type[] { typeof(Int32) }, typeof(SessionProcessRunner<Int32>), ReqParams: new Int32[] { 2, 2 });
        }

        [Fact]
        public void AddSessionProcessRunner_Config()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddSessionProcessRunner<Int32>(o => o.HostId="unknown");

            CheckInfrastructure(services, true);
            CheckTypeFactories(services, new Type[] { typeof(Func<Action<Int32, Int32?>, CancellationToken, Int32>),
                    typeof(Action<Action<Int32, Int32?>, CancellationToken>) },
                new Type[] { typeof(Int32) }, typeof(SessionProcessRunner<Int32>), ReqParams: new Int32[] { 2, 2 });
        }

    }
}
