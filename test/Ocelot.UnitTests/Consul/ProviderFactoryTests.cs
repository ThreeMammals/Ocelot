using Consul;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Shouldly;
using System;
using System.Threading;
using Xunit;

namespace Ocelot.UnitTests.Consul;

public class ProviderFactoryTests
{
    private readonly ServiceCollection _services;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IConsulClientFactory> _consulClientFactory;

    public ProviderFactoryTests()
    {
        _services = new ServiceCollection();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _loggerFactory.Setup(x => x.CreateLogger<Provider.Consul.Consul>()).Returns(_logger.Object);
        _loggerFactory.Setup(x => x.CreateLogger<PollConsul>()).Returns(_logger.Object);
        _services.AddSingleton(_loggerFactory.Object);

        _consulClientFactory = new Mock<IConsulClientFactory>();
        _services.AddSingleton(_consulClientFactory.Object);
    }

    [Fact]
    public void should_return_ConsulServiceDiscoveryProvider()
    {
        var serviceProvider = _services.BuildServiceProvider();

        var route = new DownstreamRouteBuilder()
            .WithServiceName(string.Empty)
            .Build();

        var provider = ConsulProviderFactory.Get(
            serviceProvider,
            new ServiceProviderConfiguration(string.Empty, string.Empty, string.Empty, 1, string.Empty, string.Empty, 1),
            route);

        provider.ShouldNotBeNull()
            .ShouldBeOfType<Provider.Consul.Consul>();
    }

    [Fact]
    public void should_return_PollingConsulServiceDiscoveryProvider()
    {
        var consulClient = new Mock<IConsulClient>();
        _consulClientFactory.Setup(x => x.Get(It.IsAny<ConsulRegistryConfiguration>())).Returns(consulClient.Object);

        var healthEndpoint = new Mock<IHealthEndpoint>();
        consulClient.SetupGet(x => x.Health).Returns(healthEndpoint.Object);

        healthEndpoint.Setup(x => x.Service(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<ServiceEntry[]> { Response = Array.Empty<ServiceEntry>() });

        var serviceProvider = _services.BuildServiceProvider();

        var stopsPollerFromPolling = 10000;

        var route = new DownstreamRouteBuilder()
            .WithServiceName(string.Empty)
            .Build();

        var provider = ConsulProviderFactory.Get(
            serviceProvider,
            new ServiceProviderConfiguration("pollconsul", "http", string.Empty, 1, string.Empty, string.Empty, stopsPollerFromPolling),
            route);

        provider.ShouldNotBeNull()
            .ShouldBeOfType<PollConsul>()
            .Dispose();
    }
}
