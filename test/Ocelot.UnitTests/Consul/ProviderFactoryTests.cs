using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Consul;

namespace Ocelot.UnitTests.Consul
{
    public class ProviderFactoryTests
    {
        private readonly IServiceProvider _provider;

        public ProviderFactoryTests()
        {
            var services = new ServiceCollection();
            var loggerFactory = new Mock<IOcelotLoggerFactory>();
            var logger = new Mock<IOcelotLogger>();
            loggerFactory.Setup(x => x.CreateLogger<Provider.Consul.Consul>()).Returns(logger.Object);
            loggerFactory.Setup(x => x.CreateLogger<PollConsul>()).Returns(logger.Object);
            var consulFactory = new Mock<IConsulClientFactory>();
            services.AddSingleton(consulFactory.Object);
            services.AddSingleton(loggerFactory.Object);
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void should_return_ConsulServiceDiscoveryProvider()
        {
            var route = new DownstreamRouteBuilder()
                .WithServiceName(string.Empty)
                .Build();

            var provider = ConsulProviderFactory.Get(_provider, new ServiceProviderConfiguration(string.Empty, string.Empty, string.Empty, 1, string.Empty, string.Empty, 1), route);
            provider.ShouldBeOfType<Provider.Consul.Consul>();
        }

        [Fact]
        public void should_return_PollingConsulServiceDiscoveryProvider()
        {
            var stopsPollerFromPolling = 10000;

            var route = new DownstreamRouteBuilder()
                .WithServiceName(string.Empty)
                .Build();

            var provider = ConsulProviderFactory.Get(_provider, new ServiceProviderConfiguration("pollconsul", "http", string.Empty, 1, string.Empty, string.Empty, stopsPollerFromPolling), route);
            var pollProvider = provider as PollConsul;
            pollProvider.ShouldNotBeNull();
            pollProvider.Dispose();
        }
    }
}
