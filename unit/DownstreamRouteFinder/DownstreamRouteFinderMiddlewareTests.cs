using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.DownstreamRouteFinder;

public class DownstreamRouteFinderMiddlewareTests : UnitTest
{
    private readonly Mock<IDownstreamRouteProvider> _finder;
    private readonly Mock<IDownstreamRouteProviderFactory> _factory;
    private Route _route;
    private IInternalConfiguration _config;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly DownstreamRouteFinderMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _httpContext;

    public DownstreamRouteFinderMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _finder = new Mock<IDownstreamRouteProvider>();
        _factory = new Mock<IDownstreamRouteProviderFactory>();
        _factory.Setup(x => x.Get(It.IsAny<IInternalConfiguration>())).Returns(_finder.Object);
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<DownstreamRouteFinderMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _middleware = new DownstreamRouteFinderMiddleware(_next, _loggerFactory.Object, _factory.Object);
    }

    [Fact]
    public async Task Should_call_scoped_data_repository_correctly()
    {
        // Arrange
        var config = new InternalConfiguration();
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate("any old string")
            .WithUpstreamHttpMethod(["Get"])
            .Build();
        GivenTheDownStreamRouteFinderReturns(new Route(downstreamRoute, HttpMethod.Get)); // without placeholders
        GivenTheFollowingConfig(config);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        ThenTheScopedDataRepositoryIsCalledCorrectly();
    }

    [Fact]
    [Trait("PR", "1198")]
    public async Task Should_store_errors_when_route_cannot_be_found()
    {
        // Arrange
        var errors = new List<Error> { new AnyError() };
        GivenTheFollowingConfig(new InternalConfiguration());
        _finder
            .Setup(x => x.Get(It.IsAny<HttpRequest>(), out It.Ref<IEnumerable<Error>>.IsAny))
            .Returns(new GetCallback((HttpRequest request, out IEnumerable<Error> returnedErrors) =>
            {
                returnedErrors = errors;
                return null;
            }));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        Assert.Equal(errors, _httpContext.Items.Errors());
    }

    [Theory]
    [Trait("PR", "1198")]
    [InlineData("localhost", "localhost")]
    [InlineData("localhost:5000", "localhost")]
    public void UpstreamHost_ReturnsHostNameWithoutPort(string host, string expected)
    {
        // Arrange
        _httpContext.Request.Headers.Host = host;

        // Act
        var actual = DownstreamRouteFinderMiddleware.UpstreamHost(_httpContext.Request);

        // Assert
        Assert.Equal(expected, actual);
    }

    private void GivenTheFollowingConfig(IInternalConfiguration config)
    {
        _config = config;
        _httpContext.Items.SetIInternalConfiguration(config);
    }

    private void GivenTheDownStreamRouteFinderReturns(Route route)
    {
        _route = route;
        _finder
            .Setup(x => x.Get(It.IsAny<HttpRequest>(), out It.Ref<IEnumerable<Error>>.IsAny))
            .Returns(new GetCallback((HttpRequest request, out IEnumerable<Error> errors) =>
            {
                errors = [];
                return _route;
            }));
    }
    delegate Route GetCallback(HttpRequest request, out IEnumerable<Error> errors);

    private void ThenTheScopedDataRepositoryIsCalledCorrectly()
    {
        _httpContext.Items.TemplatePlaceholderNameAndValues().ShouldBe(_route.TemplatePlaceholderNameAndValues);
        _httpContext.Items.IInternalConfiguration().ServiceProviderConfiguration.ShouldBe(_config.ServiceProviderConfiguration);
    }
}
