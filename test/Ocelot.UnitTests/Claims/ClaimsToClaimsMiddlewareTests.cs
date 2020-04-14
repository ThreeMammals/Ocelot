using Ocelot.Middleware;

namespace Ocelot.UnitTests.Claims
{
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.Claims;
    using Ocelot.Claims.Middleware;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Ocelot.Infrastructure.RequestData;
    using TestStack.BDDfy;
    using Xunit;

    public class ClaimsToClaimsMiddlewareTests
    {
        private readonly Mock<IAddClaimsToRequest> _addHeaders;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly ClaimsToClaimsMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private RequestDelegate _next;
        private HttpContext _httpContext;
        private Mock<IRequestScopedDataRepository> _repo;

        public ClaimsToClaimsMiddlewareTests()
        {
            _repo = new Mock<IRequestScopedDataRepository>();
            _httpContext = new DefaultHttpContext();
            _addHeaders = new Mock<IAddClaimsToRequest>();
            _downstreamContext = new DownstreamContext();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<ClaimsToClaimsMiddleware>()).Returns(_logger.Object);
            _next = context => Task.CompletedTask;
            _middleware = new ClaimsToClaimsMiddleware(_next, _loggerFactory.Object, _addHeaders.Object, _repo.Object);
        }

        [Fact]
        public void should_call_claims_to_request_correctly()
        {
            var downstreamRoute = new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                        .WithDownstreamPathTemplate("any old string")
                        .WithClaimsToClaims(new List<ClaimToThing>
                        {
                            new ClaimToThing("sub", "UserType", "|", 0)
                        })
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .Build())
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheAddClaimsToRequestReturns())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheClaimsToRequestIsCalledCorrectly())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_httpContext).GetAwaiter().GetResult();
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamContext.TemplatePlaceholderNameAndValues = downstreamRoute.TemplatePlaceholderNameAndValues;
            _downstreamContext.DownstreamReRoute = downstreamRoute.ReRoute.DownstreamReRoute[0];
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
}
