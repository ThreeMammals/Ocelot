using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Headers;
using Ocelot.Headers.Middleware;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Headers
{
    public class ClaimsToHeadersMiddlewareTests : UnitTest
    {
        private readonly Mock<IAddHeadersToRequest> _addHeaders;
        private Response<Ocelot.DownstreamRouteFinder.DownstreamRouteHolder> _downstreamRoute;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private readonly Mock<IOcelotLogger> _logger;
        private readonly ClaimsToHeadersMiddleware _middleware;
        private readonly RequestDelegate _next;
        private readonly HttpContext _httpContext;

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
        public void should_call_add_headers_to_request_correctly()
        {
            var downstreamRoute = new Ocelot.DownstreamRouteFinder.DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
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

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheAddHeadersToDownstreamRequestReturnsOk())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheAddHeadersToRequestIsCalledCorrectly())
                .BDDfy();
        }

        private async Task WhenICallTheMiddleware()
        {
            await _middleware.Invoke(_httpContext);
        }

        private void GivenTheDownStreamRouteIs(Ocelot.DownstreamRouteFinder.DownstreamRouteHolder downstreamRoute)
        {
            _downstreamRoute = new OkResponse<Ocelot.DownstreamRouteFinder.DownstreamRouteHolder>(downstreamRoute);

            _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);

            _httpContext.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
        }

        private void GivenTheAddHeadersToDownstreamRequestReturnsOk()
        {
            _addHeaders
                .Setup(x => x.SetHeadersOnDownstreamRequest(
                    It.IsAny<List<ClaimToThing>>(),
                    It.IsAny<IEnumerable<System.Security.Claims.Claim>>(),
                    It.IsAny<DownstreamRequest>()))
                .Returns(new OkResponse());
        }

        private void ThenTheAddHeadersToRequestIsCalledCorrectly()
        {
            _addHeaders
                .Verify(x => x.SetHeadersOnDownstreamRequest(
                    It.IsAny<List<ClaimToThing>>(),
                    It.IsAny<IEnumerable<System.Security.Claims.Claim>>(),
                    _httpContext.Items.DownstreamRequest()), Times.Once);
        }
    }
}
