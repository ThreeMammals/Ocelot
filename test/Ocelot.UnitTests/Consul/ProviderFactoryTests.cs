using Ocelot.Configuration.Builder;

namespace Ocelot.UnitTests.Consul
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Logging;
    using Provider.Consul;
    using Shouldly;
    using System;
    using Xunit;

    public class ProviderFactoryTests
    {
        private readonly IServiceProvider _provider;

        public ProviderFactoryTests()
        {
            var services = new ServiceCollection();
            var loggerFactory = new Mock<IOcelotLoggerFactory>();
            var logger = new Mock<IOcelotLogger>();
            loggerFactory.Setup(x => x.CreateLogger<Consul>()).Returns(logger.Object);
            loggerFactory.Setup(x => x.CreateLogger<PollConsul>()).Returns(logger.Object);
            var consulFactory = new Mock<IConsulClientFactory>();
            services.AddSingleton<IConsulClientFactory>(consulFactory.Object);
            services.AddSingleton<IOcelotLoggerFactory>(loggerFactory.Object);
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void should_return_ConsulServiceDiscoveryProvider()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithServiceName("")
                .Build();

            var provider = ConsulProviderFactory.Get(_provider, new ServiceProviderConfiguration("", "", 1, "", "", 1), reRoute);
            provider.ShouldBeOfType<Consul>();
        }

        [Fact]
        public void should_return_PollingConsulServiceDiscoveryProvider()
        {
            var stopsPollerFromPolling = 10000;

            var reRoute = new DownstreamReRouteBuilder()
                .WithServiceName("")
                .Build();

            var provider = ConsulProviderFactory.Get(_provider, new ServiceProviderConfiguration("pollconsul", "", 1, "", "", stopsPollerFromPolling), reRoute);
            provider.ShouldBeOfType<PollConsul>();
        }
    }
}
