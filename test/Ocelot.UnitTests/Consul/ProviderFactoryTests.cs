using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.UnitTests.Consul;

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
    public void should_return_consul_service_discovery_provider()
    {
        var route = new DownstreamRouteBuilder()
            .WithServiceName(string.Empty)
            .Build();

        var provider = ConsulProviderFactory.Get(_provider,
            new ServiceProviderConfiguration(string.Empty, string.Empty, string.Empty, 1, string.Empty, string.Empty,
                1), route);
        provider.ShouldBeOfType<Provider.Consul.Consul>();
    }

    [Fact]
    public void should_return_polling_consul_service_discovery_provider()
    {
        var provider = DummyPollingConsulServiceFactory(string.Empty);
        var pollProvider = provider as PollConsul;
        pollProvider.ShouldNotBeNull();
    }

    [Fact]
    public void should_return_same_provider_for_given_service_name()
    {
        var provider = DummyPollingConsulServiceFactory("test");
        var provider2 = DummyPollingConsulServiceFactory("test");

        provider.ShouldBeEquivalentTo(provider2);

        var pollProvider = provider as PollConsul;
        pollProvider.ShouldNotBeNull();

        var pollProvider2 = provider2 as PollConsul;
        pollProvider2.ShouldNotBeNull();

        pollProvider.ServiceName.ShouldBeEquivalentTo(pollProvider2.ServiceName);
    }

    [Theory]
    [InlineData(new object[] { new[] { "service1", "service2", "service3", "service4" } })]
    public void should_return_provider_according_to_service_name(string[] serviceNames)
    {
        var providersList = serviceNames.Select(DummyPollingConsulServiceFactory).ToList();

        foreach (var serviceName in serviceNames)
        {
            var currentProvider = DummyPollingConsulServiceFactory(serviceName);
            providersList.ShouldContain(currentProvider);
        }

        var convertedProvidersList = providersList.Select(x => x as PollConsul).ToList();

        foreach (var convertedProvider in convertedProvidersList)
        {
            convertedProvider.ShouldNotBeNull();
        }

        foreach (var serviceName in serviceNames)
        {
            var cProvider = DummyPollingConsulServiceFactory(serviceName);
            var convertedCProvider = cProvider as PollConsul;

            convertedCProvider.ShouldNotBeNull();

            var matchingProviders = convertedProvidersList
                .Where(x => x.ServiceName == convertedCProvider.ServiceName)
                .ToList();
            matchingProviders.ShouldHaveSingleItem();

            matchingProviders.First()
                .ShouldNotBeNull()
                .ServiceName.ShouldBeEquivalentTo(convertedCProvider.ServiceName);
        }
    }

    private IServiceDiscoveryProvider DummyPollingConsulServiceFactory(string serviceName)
    {
        var stopsFromPolling = 10000;

        var route = new DownstreamRouteBuilder()
            .WithServiceName(serviceName)
            .Build();

        return ConsulProviderFactory.Get?.Invoke(
            _provider,
            new ServiceProviderConfiguration(ConsulProviderFactory.PollConsul, Uri.UriSchemeHttp, string.Empty, 1, string.Empty, string.Empty, stopsFromPolling),
            route);
    }
}
