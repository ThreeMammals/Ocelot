using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamPathManipulation.Middleware;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.PathManipulation;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Values;
using System.Security.Claims;

namespace Ocelot.UnitTests.DownstreamPathManipulation
{
    public class ClaimsToDownstreamPathMiddlewareTests : UnitTest
    {
        private readonly Mock<IChangeDownstreamPathTemplate> _changePath;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private readonly Mock<IOcelotLogger> _logger;
        private readonly ClaimsToDownstreamPathMiddleware _middleware;
        private readonly RequestDelegate _next;
        private readonly HttpContext _httpContext;

        public ClaimsToDownstreamPathMiddlewareTests()
        {
            _httpContext = new DefaultHttpContext();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<ClaimsToDownstreamPathMiddleware>()).Returns(_logger.Object);
            _next = context => Task.CompletedTask;
            _changePath = new Mock<IChangeDownstreamPathTemplate>();
            _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com")));
            _middleware = new ClaimsToDownstreamPathMiddleware(_next, _loggerFactory.Object, _changePath.Object);
        }

        [Fact]
        public void should_call_add_queries_correctly()
        {
            var downstreamRoute = new Ocelot.DownstreamRouteFinder.DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
                new RouteBuilder()
                    .WithDownstreamRoute(new DownstreamRouteBuilder()
                        .WithDownstreamPathTemplate("any old string")
                        .WithClaimsToDownstreamPath(new List<ClaimToThing>
                        {
                            new("UserId", "Subject", string.Empty, 0),
                        })
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .Build())
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
               .And(x => x.GivenTheChangeDownstreamPathReturnsOk())
               .When(x => x.WhenICallTheMiddleware())
               .Then(x => x.ThenChangeDownstreamPathIsCalledCorrectly())
               .BDDfy();
        }

        private async Task WhenICallTheMiddleware()
        {
            await _middleware.Invoke(_httpContext);
        }

        private void GivenTheChangeDownstreamPathReturnsOk()
        {
            _changePath
                .Setup(x => x.ChangeDownstreamPath(
                    It.IsAny<List<ClaimToThing>>(),
                    It.IsAny<IEnumerable<Claim>>(),
                    It.IsAny<DownstreamPathTemplate>(),
                    It.IsAny<List<PlaceholderNameAndValue>>()))
                .Returns(new OkResponse());
        }

        private void ThenChangeDownstreamPathIsCalledCorrectly()
        {
            _changePath
                .Verify(x => x.ChangeDownstreamPath(
                    It.IsAny<List<ClaimToThing>>(),
                    It.IsAny<IEnumerable<Claim>>(),
                    _httpContext.Items.DownstreamRoute().DownstreamPathTemplate,
                    _httpContext.Items.TemplatePlaceholderNameAndValues()), Times.Once);
        }

        private void GivenTheDownStreamRouteIs(Ocelot.DownstreamRouteFinder.DownstreamRouteHolder downstreamRoute)
        {
            _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);

            _httpContext.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
        }
    }
}
