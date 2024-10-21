using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Consul.Interfaces;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.UnitTests.Consul;

public class ProviderFactoryTests
{
    private readonly IServiceProvider _provider;
    private readonly HttpContext _context = new DefaultHttpContext();

    public ProviderFactoryTests()
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        _context.Items.Add(nameof(ConsulRegistryConfiguration), new ConsulRegistryConfiguration(null, null, 0, null, null));
        contextAccessor.SetupGet(x => x.HttpContext).Returns(_context);

        var loggerFactory = new Mock<IOcelotLoggerFactory>();
        var logger = new Mock<IOcelotLogger>();
        loggerFactory.Setup(x => x.CreateLogger<Provider.Consul.Consul>()).Returns(logger.Object);
        loggerFactory.Setup(x => x.CreateLogger<PollConsul>()).Returns(logger.Object);

        var consulFactory = new Mock<IConsulClientFactory>();
        var consulServiceBuilder = new Mock<IConsulServiceBuilder>();

        var services = new ServiceCollection();
        services.AddSingleton(contextAccessor.Object);
        services.AddSingleton(consulFactory.Object);
        services.AddSingleton(loggerFactory.Object);
        services.AddScoped(_ => consulServiceBuilder.Object);

        _provider = services.BuildServiceProvider(true); // validate scopes!!!
        _context.RequestServices = _provider.CreateScope().ServiceProvider;
    }

    [Fact]
    public void Get_EmptyServiceName_ReturnedConsul()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithServiceName(string.Empty)
            .Build();

        // Act
        var actual = ConsulProviderFactory.Get(
            _provider,
            new ServiceProviderConfiguration(string.Empty, string.Empty, string.Empty, 1, string.Empty, string.Empty, 1),
            route);

        // Assert
        actual.ShouldBeOfType<Provider.Consul.Consul>();
    }

    [Fact]
    public void Get_EmptyServiceName_ReturnedPollConsul()
    {
        // Arrange, Act
        var route = GivenRoute(string.Empty);
        var actual = Act(route);

        // Assert
        actual.ShouldNotBeNull().ShouldBeOfType<PollConsul>();
    }

    [Fact]
    public void Get_RoutesWithTheSameServiceName_ReturnedSameProvider()
    {
        // Arrange, Act: 1
        var route1 = GivenRoute("test");
        var actual1 = Act(route1);

        // Arrange, Act: 2
        var route2 = GivenRoute("test");
        var actual2 = Act(route2);

        // Assert
        actual1.ShouldNotBeNull().ShouldBeOfType<PollConsul>();
        actual2.ShouldNotBeNull().ShouldBeOfType<PollConsul>();
        actual1.ShouldBeEquivalentTo(actual2);
        var provider1 = actual1 as PollConsul;
        var provider2 = actual2 as PollConsul;
        provider1.ServiceName.ShouldBeEquivalentTo(provider2.ServiceName);
    }

    [Fact]
    public void ShouldReturnProviderAccordingToServiceName()
    {
        string[] serviceNames = new[] { "service1", "service2", "service3", "service4" };
        var providersList = serviceNames.Select(DummyPollingConsulServiceFactory).ToList();

        foreach (var serviceName in serviceNames)
        {
            var currentProvider = DummyPollingConsulServiceFactory(serviceName);
            providersList.ShouldContain(currentProvider);
        }

        var convertedProvidersList = providersList.Select(x => x as PollConsul).ToList();
        convertedProvidersList.ForEach(x => x.ShouldNotBeNull());

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

    [Fact]
    [Trait("Bug", "2178")]
    public void Should_throw_invalid_operation_exception()
    {
        // Arrange
        var route = GivenRoute(string.Empty);
        _context.RequestServices = _provider; // given service provider is root provider

        // Act
        Func<IServiceDiscoveryProvider> consulProviderFactoryCall = () => Act(route);

        // Assert
        consulProviderFactoryCall.ShouldThrow<InvalidOperationException>();
    }

    private IServiceDiscoveryProvider DummyPollingConsulServiceFactory(string serviceName) => Act(GivenRoute(serviceName));

    private static DownstreamRoute GivenRoute(string serviceName) => new DownstreamRouteBuilder()
        .WithServiceName(serviceName)
        .Build();

    private IServiceDiscoveryProvider Act(DownstreamRoute route)
    {
        var stopsFromPolling = 10000;
        return ConsulProviderFactory.Get?.Invoke(
            _provider,
            new ServiceProviderConfiguration(ConsulProviderFactory.PollConsul, Uri.UriSchemeHttp, string.Empty, 1, string.Empty, string.Empty, stopsFromPolling),
            route);
    }
}
