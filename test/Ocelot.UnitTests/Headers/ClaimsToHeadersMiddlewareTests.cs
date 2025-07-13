using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Headers;
using Ocelot.Headers.Middleware;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.UnitTests.Headers;

public class ClaimsToHeadersMiddlewareTests : UnitTest
{
    private readonly Mock<IAddHeadersToRequest> _addHeaders;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly ClaimsToHeadersMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _httpContext;

    public ClaimsToHeadersMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _addHeaders = new Mock<IAddHeadersToRequest>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<ClaimsToHeadersMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _middleware = new ClaimsToHeadersMiddleware(_next, _loggerFactory.Object, _addHeaders.Object);
        _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com")));
    }

    [Fact]
    public async Task Should_call_add_headers_to_request_correctly()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteHolder(new(),
            new RouteBuilder()
                .WithDownstreamRoute(new DownstreamRouteBuilder()
                        .WithDownstreamPathTemplate("any old string")
                        .WithClaimsToHeaders(new List<ClaimToThing>
                        {
                            new("UserId", "Subject", string.Empty, 0),
                        })
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .Build())
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build());

        // Arrange: Given The Down Stream Route Is
        _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);
        _httpContext.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);

        // Arrange: Given The AddHeaders To Downstream Request Returns Ok
        _addHeaders.Setup(x => x.SetHeadersOnDownstreamRequest(It.IsAny<List<ClaimToThing>>(), It.IsAny<IEnumerable<Claim>>(), It.IsAny<DownstreamRequest>()))
            .Returns(new OkResponse());

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert: Then The AddHeaders ToRequest Is Called Correctly
        _addHeaders.Verify(
            x => x.SetHeadersOnDownstreamRequest(It.IsAny<List<ClaimToThing>>(), It.IsAny<IEnumerable<Claim>>(), _httpContext.Items.DownstreamRequest()),
            Times.Once);
    }
}
