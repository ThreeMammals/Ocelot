using Microsoft.AspNetCore.Http;
using Ocelot.Claims;
using Ocelot.Claims.Middleware;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Claims;

public class ClaimsToClaimsMiddlewareTests : UnitTest
{
    private readonly Mock<IAddClaimsToRequest> _addHeaders;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly ClaimsToClaimsMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _httpContext;

    public ClaimsToClaimsMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _addHeaders = new Mock<IAddClaimsToRequest>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<ClaimsToClaimsMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _middleware = new ClaimsToClaimsMiddleware(_next, _loggerFactory.Object, _addHeaders.Object);
    }

    [Fact]
    public async Task Should_call_claims_to_request_correctly()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>(),
            new RouteBuilder()
                .WithDownstreamRoute(new DownstreamRouteBuilder()
                    .WithDownstreamPathTemplate("any old string")
                    .WithClaimsToClaims(new()
                    {
                        new("sub", "UserType", "|", 0),
                    })
                    .Build())
                .WithUpstreamHttpMethod(new() { HttpMethods.Get })
                .Build());

        GivenTheDownStreamRouteIs(downstreamRoute);
        GivenTheAddClaimsToRequestReturns();

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        ThenTheClaimsToRequestIsCalledCorrectly();
    }

    private void GivenTheDownStreamRouteIs(Ocelot.DownstreamRouteFinder.DownstreamRouteHolder downstreamRoute)
    {
        _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);

        _httpContext.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
    }

    private void GivenTheAddClaimsToRequestReturns()
    {
        _addHeaders
            .Setup(x => x.SetClaimsOnContext(It.IsAny<List<ClaimToThing>>(),
            It.IsAny<HttpContext>()))
            .Returns(new OkResponse());
    }

    private void ThenTheClaimsToRequestIsCalledCorrectly()
    {
        _addHeaders
            .Verify(x => x.SetClaimsOnContext(It.IsAny<List<ClaimToThing>>(),
            It.IsAny<HttpContext>()), Times.Once);
    }
}
