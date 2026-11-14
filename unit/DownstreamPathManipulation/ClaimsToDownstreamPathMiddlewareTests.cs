using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamPathManipulation;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;
using Ocelot.Values;
using System.Security.Claims;

namespace Ocelot.UnitTests.DownstreamPathManipulation;

[Trait("Feat", "968")] // https://github.com/ThreeMammals/Ocelot/pull/968
[Trait("Release", "13.8.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/13.8.0
public class ClaimsToDownstreamPathMiddlewareTests : UnitTest
{
    private readonly Mock<IChangeDownstreamPathTemplate> _changePath;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly ClaimsToDownstreamPathMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _httpContext;
    private bool _nextCalled;

    public ClaimsToDownstreamPathMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<ClaimsToDownstreamPathMiddleware>()).Returns(_logger.Object);
        _next = context =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        };
        _changePath = new Mock<IChangeDownstreamPathTemplate>();
        _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com")));
        _middleware = new ClaimsToDownstreamPathMiddleware(_next, _loggerFactory.Object, _changePath.Object);
    }

    [Fact]
    public async Task Should_call_add_queries_correctly()
    {
        // Arrange
        var dr = new DownstreamRouteBuilder()
                    .WithDownstreamPathTemplate("any old string")
                    .WithClaimsToDownstreamPath(
                    [
                        new("UserId", "Subject", string.Empty, 0),
                    ])
                    .WithUpstreamHttpMethod(["Get"])
                    .Build();
        var route = new Route(dr, HttpMethod.Get);

        // Arrange: Given The Down Stream Route Is
        _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(route.TemplatePlaceholderNameAndValues);
        _httpContext.Items.UpsertDownstreamRoute(route.DownstreamRoute[0]);

        // Arrange: Given The Change Downstream Path Returns Ok
        _changePath.Setup(x => x.ChangeDownstreamPath(
                It.IsAny<List<ClaimToThing>>(),
                It.IsAny<IEnumerable<Claim>>(),
                It.IsAny<DownstreamPathTemplate>(),
                It.IsAny<List<PlaceholderNameAndValue>>()))
            .Returns(new OkResponse());

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        _changePath.Verify(x => x.ChangeDownstreamPath(
                It.IsAny<List<ClaimToThing>>(),
                It.IsAny<IEnumerable<Claim>>(),
                _httpContext.Items.DownstreamRoute().DownstreamPathTemplate,
                _httpContext.Items.TemplatePlaceholderNameAndValues()), Times.Once);
    }

    [Fact]
    [Trait("PR", "1198")]
    public async Task Should_call_next_when_route_has_no_claims_to_path()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate("/api")
            .WithUpstreamHttpMethod(["Get"])
            .Build();
        _httpContext.Items.UpsertDownstreamRoute(downstreamRoute);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        Assert.True(_nextCalled);
        _changePath.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("PR", "1198")]
    public async Task Should_store_errors_when_claim_path_change_fails()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate("/api")
            .WithClaimsToDownstreamPath([new("UserId", "Subject", string.Empty, 0)])
            .WithUpstreamHttpMethod(["Get"])
            .Build();
        _httpContext.Items.UpsertDownstreamRoute(downstreamRoute);
        _changePath
            .Setup(x => x.ChangeDownstreamPath(
                It.IsAny<List<ClaimToThing>>(),
                It.IsAny<IEnumerable<Claim>>(),
                It.IsAny<DownstreamPathTemplate>(),
                It.IsAny<List<PlaceholderNameAndValue>>()))
            .Returns(new ErrorResponse(new AnyError()));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        Assert.False(_nextCalled);
        Assert.NotEmpty(_httpContext.Items.Errors());
    }
}
