using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.QueryStrings;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.UnitTests.QueryStrings;

/// <summary>
/// Feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/claimstransformation.rst#claims-to-query-string-parameters">Claims to Query String Parameters</see>.
/// </summary>
[Trait("Commit", "f7f4a39")] // https://github.com/ThreeMammals/Ocelot/commit/f7f4a392f0743b38cd0206a81b4c094e60fe7b93
[Trait("Release", "1.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0-beta.1 -> https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0
public class ClaimsToQueryStringMiddlewareTests : UnitTest
{
    private readonly Mock<IAddQueriesToRequest> _addQueries;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly ClaimsToQueryStringMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _httpContext;

    public ClaimsToQueryStringMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<ClaimsToQueryStringMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _addQueries = new Mock<IAddQueriesToRequest>();
        _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com")));
        _middleware = new ClaimsToQueryStringMiddleware(_next, _loggerFactory.Object, _addQueries.Object);
    }

    [Fact]
    public async Task Should_call_add_queries_correctly()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate("any old string")
            .WithClaimsToQueries(new List<ClaimToThing>
            {
                new("UserId", "Subject", string.Empty, 0),
            })
            .WithUpstreamHttpMethod(new List<string> { "Get" })
            .Build();
        var downstreamRoute = new DownstreamRouteHolder(
            new(),
            new Route(route, HttpMethod.Get));
        _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);
        _httpContext.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
        _addQueries.Setup(x => x.SetQueriesOnDownstreamRequest(It.IsAny<List<ClaimToThing>>(), It.IsAny<IEnumerable<Claim>>(), It.IsAny<DownstreamRequest>()))
            .Returns(new OkResponse());

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        _addQueries.Verify(x => x.SetQueriesOnDownstreamRequest(It.IsAny<List<ClaimToThing>>(), It.IsAny<IEnumerable<Claim>>(), _httpContext.Items.DownstreamRequest()),
            Times.Once);
    }
}
