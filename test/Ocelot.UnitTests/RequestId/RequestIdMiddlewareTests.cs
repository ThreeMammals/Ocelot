using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.RequestId.Middleware;
using Ocelot.Responses;

namespace Ocelot.UnitTests.RequestId;

public class RequestIdMiddlewareTests : UnitTest
{
    private readonly HttpRequestMessage _downstreamRequest;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly RequestIdMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly Mock<IRequestScopedDataRepository> _repo;
    private readonly DefaultHttpContext _httpContext;
    public RequestIdMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _downstreamRequest = new HttpRequestMessage(HttpMethod.Get, "http://test.com");
        _repo = new Mock<IRequestScopedDataRepository>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<RequestIdMiddleware>()).Returns(_logger.Object);
        _next = context =>
        {
            _httpContext.Response.Headers.Append("LSRequestId", _httpContext.TraceIdentifier);
            return Task.CompletedTask;
        };
        _middleware = new RequestIdMiddleware(_next, _loggerFactory.Object, _repo.Object);
        _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(_downstreamRequest));
    }

    [Fact]
    public async Task Should_pass_down_request_id_from_upstream_request()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
            new RouteBuilder()
                .WithDownstreamRoute(new DownstreamRouteBuilder()
                    .WithDownstreamPathTemplate("any old string")
                    .WithRequestIdKey("LSRequestId")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())
            .WithUpstreamHttpMethod(new List<string> { "Get" })
            .Build());

        var requestId = Guid.NewGuid().ToString();

        GivenTheDownStreamRouteIs(downstreamRoute);
        GivenThereIsNoGlobalRequestId();
        _httpContext.Request.Headers.TryAdd("LSRequestId", requestId);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        ThenTheTraceIdIs(requestId);
    }

    [Fact]
    public async Task Should_add_request_id_when_not_on_upstream_request()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
            new RouteBuilder()
                .WithDownstreamRoute(new DownstreamRouteBuilder()
                    .WithDownstreamPathTemplate("any old string")
                    .WithRequestIdKey("LSRequestId")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build());

        GivenTheDownStreamRouteIs(downstreamRoute);
        GivenThereIsNoGlobalRequestId();

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert: Then The TraceId Is Anything
        _httpContext.Response.Headers.TryGetValue("LSRequestId", out var value);
        value.First().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_add_request_id_scoped_repo_for_logging_later()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
            new RouteBuilder()
                .WithDownstreamRoute(new DownstreamRouteBuilder()
                    .WithDownstreamPathTemplate("any old string")
                    .WithRequestIdKey("LSRequestId")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build());

        var requestId = Guid.NewGuid().ToString();

        GivenTheDownStreamRouteIs(downstreamRoute);
        GivenThereIsNoGlobalRequestId();
        _httpContext.Request.Headers.TryAdd("LSRequestId", requestId);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        ThenTheTraceIdIs(requestId);
        _repo.Verify(x => x.Add("RequestId", requestId), Times.Once);
    }

    [Fact]
    public async Task Should_update_request_id_scoped_repo_for_logging_later()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
            new RouteBuilder()
                .WithDownstreamRoute(new DownstreamRouteBuilder()
                    .WithDownstreamPathTemplate("any old string")
                    .WithRequestIdKey("LSRequestId")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build());

        var requestId = Guid.NewGuid().ToString();

        GivenTheDownStreamRouteIs(downstreamRoute);
        GivenTheRequestIdWasSetGlobally();
        _httpContext.Request.Headers.TryAdd("LSRequestId", requestId);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        ThenTheTraceIdIs(requestId);
        _repo.Verify(x => x.Update("RequestId", requestId), Times.Once);
    }

    [Fact]
    public async Task Should_not_update_if_global_request_id_is_same_as_re_route_request_id()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
            new RouteBuilder()
                .WithDownstreamRoute(new DownstreamRouteBuilder()
                    .WithDownstreamPathTemplate("any old string")
                    .WithRequestIdKey("LSRequestId")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build());

        var requestId = "alreadyset";

        GivenTheDownStreamRouteIs(downstreamRoute);
        GivenTheRequestIdWasSetGlobally();
        _httpContext.Request.Headers.TryAdd("LSRequestId", requestId);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        ThenTheTraceIdIs(requestId);
        _repo.Verify(x => x.Update("RequestId", requestId), Times.Never);
    }

    private void GivenThereIsNoGlobalRequestId()
    {
        _repo.Setup(x => x.Get<string>("RequestId")).Returns(new OkResponse<string>(null));
    }

    private void GivenTheRequestIdWasSetGlobally()
    {
        _repo.Setup(x => x.Get<string>("RequestId")).Returns(new OkResponse<string>("alreadyset"));
    }

    private void GivenTheDownStreamRouteIs(DownstreamRouteHolder downstreamRoute)
    {
        _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);
        _httpContext.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
    }

    private void ThenTheTraceIdIs(string expected)
    {
        _httpContext.Response.Headers.TryGetValue("LSRequestId", out var value);
        value.First().ShouldBe(expected);
    }
}
