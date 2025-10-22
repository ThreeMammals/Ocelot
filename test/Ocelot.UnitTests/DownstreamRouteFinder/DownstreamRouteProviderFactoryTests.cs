using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;

namespace Ocelot.UnitTests.DownstreamRouteFinder;

using Ocelot.DependencyInjection;
using Ocelot.DownstreamRouteFinder.Finder;

public class DownstreamRouteProviderFactoryTests : UnitTest
{
    private readonly DownstreamRouteProviderFactory _factory;
    private IInternalConfiguration _config;
    private IDownstreamRouteProvider _result;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;

    public DownstreamRouteProviderFactoryTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPlaceholderNameAndValueFinder, UrlPathPlaceholderNameAndValueFinder>();
        services.AddSingleton<IHeaderPlaceholderNameAndValueFinder, HeaderPlaceholderNameAndValueFinder>();
        services.AddSingleton<IUrlPathToUrlTemplateMatcher, RegExUrlMatcher>();
        services.AddSingleton<IHeadersToHeaderTemplatesMatcher, HeadersToHeaderTemplatesMatcher>();
        services.AddSingleton<IQoSOptionsCreator, QoSOptionsCreator>();
        services.AddSingleton<IRouteKeyCreator, RouteKeyCreator>();
        services.AddSingleton<IDownstreamRouteProvider, DownstreamRouteFinder>();
        services.AddSingleton<IDownstreamRouteProvider, DiscoveryDownstreamRouteFinder>();
        Features.AddHeaderRouting(services); // AddSingleton<IUpstreamHeaderTemplatePatternCreator, UpstreamHeaderTemplatePatternCreator>()
        var provider = services.BuildServiceProvider(true);
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _loggerFactory.Setup(x => x.CreateLogger<DownstreamRouteProviderFactory>()).Returns(_logger.Object);
        _factory = new DownstreamRouteProviderFactory(provider, _loggerFactory.Object);
    }

    [Fact]
    public void Should_return_downstream_route_finder()
    {
        // Arrange
        var route = new Route();
        GivenTheRoutes(route);

        // Act
        _result = _factory.Get(_config);

        // Assert
        _result.ShouldBeOfType<DownstreamRouteFinder>();
    }

    [Fact]
    public void Should_return_downstream_route_finder_when_not_dynamic_re_route_and_service_discovery_on()
    {
        // Arrange
        var route = new Route()
        {
            UpstreamTemplatePattern = new UpstreamPathTemplateBuilder().WithOriginalValue("woot").Build(),
        };
        var spConfig = new ServiceProviderConfigurationBuilder()
            .WithScheme("http").WithHost("test").WithPort(50).WithType("test").Build();
        GivenTheRoutes(route, spConfig);

        // Act
        _result = _factory.Get(_config);

        // Assert
        _result.ShouldBeOfType<DownstreamRouteFinder>();
    }

    [Fact]
    public void Should_return_downstream_route_finder_as_no_service_discovery_given_no_scheme()
    {
        // Arrange
        var spConfig = new ServiceProviderConfigurationBuilder()
            .WithScheme(string.Empty).WithHost("test").WithPort(50).Build();
        GivenTheRoutes(null, spConfig);

        // Act
        _result = _factory.Get(_config);

        // Assert
        _result.ShouldBeOfType<DownstreamRouteFinder>();
    }

    [Fact]
    public void Should_return_downstream_route_finder_as_no_service_discovery_given_no_host()
    {
        // Arrange
        var spConfig = new ServiceProviderConfigurationBuilder()
            .WithScheme("http").WithHost(string.Empty).WithPort(50).Build();
        GivenTheRoutes(null, spConfig);

        // Act
        _result = _factory.Get(_config);

        // Assert
        _result.ShouldBeOfType<DownstreamRouteFinder>();
    }

    [Fact]
    public void Should_return_downstream_route_finder_given_no_service_discovery_port()
    {
        // Arrange
        var spConfig = new ServiceProviderConfigurationBuilder()
            .WithScheme("http").WithHost("localhost").WithPort(0).Build();
        GivenTheRoutes(null, spConfig);

        // Act
        _result = _factory.Get(_config);

        // Assert
        _result.ShouldBeOfType<DownstreamRouteFinder>();
    }

    [Fact]
    public void Should_return_downstream_route_finder_given_no_service_discovery_type()
    {
        // Arrange
        var spConfig = new ServiceProviderConfigurationBuilder()
            .WithScheme("http").WithHost("localhost").WithPort(50).WithType(string.Empty).Build();
        GivenTheRoutes(null, spConfig);

        // Act
        _result = _factory.Get(_config);

        // Assert
        _result.ShouldBeOfType<DownstreamRouteFinder>();
    }

    [Fact]
    public void Should_return_downstream_route_creator()
    {
        // Arrange
        var spConfig = new ServiceProviderConfigurationBuilder()
            .WithScheme("http").WithHost("test").WithPort(50).WithType("test").Build();
        GivenTheRoutes(null, spConfig);

        // Act
        _result = _factory.Get(_config);

        // Assert
        _result.ShouldBeOfType<DiscoveryDownstreamRouteFinder>();
    }

    [Fact]
    public void Should_return_downstream_route_creator_with_dynamic_re_route()
    {
        // Arrange
        var route = new Route();
        var spConfig = new ServiceProviderConfigurationBuilder()
            .WithScheme("http").WithHost("test").WithPort(50).WithType("test").Build();
        GivenTheRoutes(route, spConfig);

        // Act
        _result = _factory.Get(_config);

        // Assert
        _result.ShouldBeOfType<DiscoveryDownstreamRouteFinder>();
    }

    private void GivenTheRoutes(Route route, ServiceProviderConfiguration config = null)
    {
        List<Route> routes = route == null ? new() : new() { route };
        _config = new InternalConfiguration(routes)
        {
            ServiceProviderConfiguration = config,
        };
    }
}
