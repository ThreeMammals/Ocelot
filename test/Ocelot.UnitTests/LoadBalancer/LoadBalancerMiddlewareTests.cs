using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.LoadBalancer.Middleware;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.UnitTests.LoadBalancer;

public class LoadBalancerMiddlewareTests : UnitTest
{
    private readonly Mock<ILoadBalancerHouse> _loadBalancerHouse;
    private readonly Mock<ILoadBalancer> _loadBalancer;
    private ServiceHostAndPort _hostAndPort;
    private ErrorResponse<ILoadBalancer> _getLoadBalancerHouseError;
    private ErrorResponse<ServiceHostAndPort> _getHostAndPortError;
    private readonly HttpRequestMessage _downstreamRequest;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private LoadBalancingMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _httpContext;

    public LoadBalancerMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _loadBalancer = new Mock<ILoadBalancer>();
        _loadBalancerHouse = new Mock<ILoadBalancerHouse>();
        _downstreamRequest = new HttpRequestMessage(HttpMethod.Get, "http://test.com/");
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<LoadBalancingMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;

        _loadBalancerHouse.Setup(x => x.Get(It.IsAny<DownstreamRoute>(), It.IsAny<ServiceProviderConfiguration>()))
            .Returns(new OkResponse<ILoadBalancer>(_loadBalancer.Object));
    }

    [Fact]
    public async Task Should_call_scoped_data_repository_correctly()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .Build();
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder()
            .Build();
        GivenTheDownStreamUrlIs("http://my.url/abc?q=123");
        GivenTheConfigurationIs(serviceProviderConfig);
        GivenTheDownStreamRouteIs(downstreamRoute, new List<PlaceholderNameAndValue>());

        // Arrange: Given The Load Balancer Returns
        _hostAndPort = new ServiceHostAndPort("127.0.0.1", 80);
        _loadBalancer.Setup(x => x.LeaseAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new OkResponse<ServiceHostAndPort>(_hostAndPort));

        // Act
        _middleware = new LoadBalancingMiddleware(_next, _loggerFactory.Object, _loadBalancerHouse.Object);
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Items.DownstreamRequest().ToHttpRequestMessage().RequestUri.OriginalString.ShouldBe("http://127.0.0.1:80/abc?q=123");
    }

    [Fact]
    public async Task Should_set_pipeline_error_if_cannot_get_load_balancer()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .Build();
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder()
            .Build();
        GivenTheDownStreamUrlIs("http://my.url/abc?q=123");
        GivenTheConfigurationIs(serviceProviderConfig);
        GivenTheDownStreamRouteIs(downstreamRoute, new List<PlaceholderNameAndValue>());

        // Arrange: Given The Load Balancer House Returns An Error
        _getLoadBalancerHouseError = new ErrorResponse<ILoadBalancer>(new List<Error>
        {
            new UnableToFindLoadBalancerError("unabe to find load balancer for bah"),
        });
        _loadBalancerHouse.Setup(x => x.Get(It.IsAny<DownstreamRoute>(), It.IsAny<ServiceProviderConfiguration>()))
            .Returns(_getLoadBalancerHouseError);

        // Act
        _middleware = new LoadBalancingMiddleware(_next, _loggerFactory.Object, _loadBalancerHouse.Object);
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Items.Errors().Count.ShouldBeGreaterThan(0);
        _httpContext.Items.Errors().ShouldBe(_getLoadBalancerHouseError.Errors);
    }

    [Fact]
    public async Task Should_set_pipeline_error_if_cannot_get_least()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .Build();
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder()
           .Build();
        GivenTheDownStreamUrlIs("http://my.url/abc?q=123");
        GivenTheConfigurationIs(serviceProviderConfig);
        GivenTheDownStreamRouteIs(downstreamRoute, new List<PlaceholderNameAndValue>());

        // Arrange: Given The Load Balancer Returns An Error
        _getHostAndPortError = new ErrorResponse<ServiceHostAndPort>(new List<Error> { new ServicesAreNullError("services were null for bah") });
        _loadBalancer.Setup(x => x.LeaseAsync(It.IsAny<HttpContext>()))
           .ReturnsAsync(_getHostAndPortError);

        // Act
        _middleware = new LoadBalancingMiddleware(_next, _loggerFactory.Object, _loadBalancerHouse.Object);
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Items.Errors().Count.ShouldBeGreaterThan(0);
        _httpContext.Items.Errors().ShouldBe(_getHostAndPortError.Errors);
    }

    [Fact]
    public async Task Should_set_scheme()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .Build();
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder()
            .Build();
        GivenTheDownStreamUrlIs("http://my.url/abc?q=123");
        GivenTheConfigurationIs(serviceProviderConfig);
        GivenTheDownStreamRouteIs(downstreamRoute, new List<PlaceholderNameAndValue>());

        // Arrange: Given The Load Balancer Returns Ok
        _loadBalancer.Setup(x => x.LeaseAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("abc", 123, "https")));

        // Act
        _middleware = new LoadBalancingMiddleware(_next, _loggerFactory.Object, _loadBalancerHouse.Object);
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Items.DownstreamRequest().Host.ShouldBeEquivalentTo("abc");
        _httpContext.Items.DownstreamRequest().Port.ShouldBeEquivalentTo(123);
        _httpContext.Items.DownstreamRequest().Scheme.ShouldBeEquivalentTo("https");
    }

    private void GivenTheConfigurationIs(ServiceProviderConfiguration config)
    {
        var configuration = new InternalConfiguration(null, null, config, null, null, null, null, null, null, null);
        _httpContext.Items.SetIInternalConfiguration(configuration);
    }

    private void GivenTheDownStreamUrlIs(string downstreamUrl)
    {
        _downstreamRequest.RequestUri = new Uri(downstreamUrl);
        _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(_downstreamRequest));
    }

    private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute, List<PlaceholderNameAndValue> placeholder)
    {
        _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(placeholder);
        _httpContext.Items.UpsertDownstreamRoute(downstreamRoute);
    }
}
