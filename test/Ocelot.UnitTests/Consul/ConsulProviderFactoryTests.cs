using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Consul.Interfaces;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.UnitTests.Consul;

public sealed class ConsulProviderFactoryTests : UnitTest, IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly DefaultHttpContext _context = new();

    public ConsulProviderFactoryTests()
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
        _scope = _provider.CreateScope();
        _context.RequestServices = _scope.ServiceProvider;
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public void Get_EmptyTypeName_ReturnedConsul()
    {
        // Arrange
        var emptyType = string.Empty;
        var route = GivenRoute(string.Empty);

        // Act
        var actual = CreateProvider(route, emptyType);

        // Assert
        actual.ShouldNotBeNull().ShouldBeOfType<Provider.Consul.Consul>();
    }

    [Fact]
    public void Get_PollConsulTypeName_ReturnedPollConsul()
    {
        // Arrange, Act
        var route = GivenRoute(string.Empty);
        var actual = CreateProvider(route, nameof(PollConsul));

        // Assert
        actual.ShouldNotBeNull().ShouldBeOfType<PollConsul>();
    }

    [Fact]
    public void Get_RoutesWithTheSameServiceName_ReturnedSameProvider()
    {
        // Arrange, Act: 1
        var route1 = GivenRoute("test");
        var actual1 = CreateProvider(route1);

        // Arrange, Act: 2
        var route2 = GivenRoute("test");
        var actual2 = CreateProvider(route2);

        // Assert
        actual1.ShouldNotBeNull().ShouldBeOfType<PollConsul>();
        actual2.ShouldNotBeNull().ShouldBeOfType<PollConsul>();
        actual1.ShouldBeEquivalentTo(actual2);
        var provider1 = actual1 as PollConsul;
        var provider2 = actual2 as PollConsul;
        provider1.ServiceName.ShouldBeEquivalentTo(provider2.ServiceName);
    }

    [Fact]
    public void Get_MultipleServiceNames_ShouldReturnProviderAccordingToServiceName()
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
    public void Get_RootProvider_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var route = GivenRoute(string.Empty);
        _context.RequestServices = _provider; // given service provider is root provider

        // Act
        Func<IServiceDiscoveryProvider> consulProviderFactoryCall = () => CreateProvider(route);

        // Assert
        consulProviderFactoryCall.ShouldThrow<InvalidOperationException>();
    }

    private IServiceDiscoveryProvider DummyPollingConsulServiceFactory(string serviceName) => CreateProvider(GivenRoute(serviceName));

    private static DownstreamRoute GivenRoute(string serviceName) => new DownstreamRouteBuilder()
        .WithServiceName(serviceName)
        .Build();

    private IServiceDiscoveryProvider CreateProvider(DownstreamRoute route, string providerType = ConsulProviderFactory.PollConsul)
    {
        var stopsFromPolling = 10000;
        return ConsulProviderFactory.Get.Invoke(
            _provider,
            new ServiceProviderConfiguration(providerType, Uri.UriSchemeHttp, string.Empty, 1, string.Empty, string.Empty, stopsFromPolling),
            route);
    }
}
