using Ocelot.Middleware;

namespace Ocelot.UnitTests.QueryStrings
{
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.QueryStrings;
    using Ocelot.QueryStrings.Middleware;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Builder;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Microsoft.AspNetCore.Http;

    public class QueryStringBuilderMiddlewareTests
    {
        private readonly Mock<IAddQueriesToRequest> _addQueries;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private QueryStringBuilderMiddleware _middleware;
        private DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;

        public QueryStringBuilderMiddlewareTests()
        {
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<QueryStringBuilderMiddleware>()).Returns(_logger.Object);
            _next = async context => {
                //do nothing
            };
            _addQueries = new Mock<IAddQueriesToRequest>();
            _downstreamContext.DownstreamRequest = new HttpRequestMessage();
            _middleware = new QueryStringBuilderMiddleware(_next, _loggerFactory.Object, _addQueries.Object);
        }

        [Fact]
        public void should_call_add_queries_correctly()
        {
            var downstreamRoute = new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                        .WithDownstreamPathTemplate("any old string")
                        .WithClaimsToQueries(new List<ClaimToThing>
                        {
                            new ClaimToThing("UserId", "Subject", "", 0)
                        })
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .Build())
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheAddHeadersToRequestReturnsOk())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheAddQueriesToRequestIsCalledCorrectly())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheAddHeadersToRequestReturnsOk()
        {
            _addQueries
                .Setup(x => x.SetQueriesOnDownstreamRequest(
                    It.IsAny<List<ClaimToThing>>(),
                    It.IsAny<IEnumerable<Claim>>(),
                    It.IsAny<HttpRequestMessage>()))
                .Returns(new OkResponse());
        }

        private void ThenTheAddQueriesToRequestIsCalledCorrectly()
        {
            _addQueries
                .Verify(x => x.SetQueriesOnDownstreamRequest(
                    It.IsAny<List<ClaimToThing>>(),
                    It.IsAny<IEnumerable<Claim>>(),
                    _downstreamContext.DownstreamRequest), Times.Once);
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamContext.TemplatePlaceholderNameAndValues = downstreamRoute.TemplatePlaceholderNameAndValues;
            _downstreamContext.DownstreamReRoute = downstreamRoute.ReRoute.DownstreamReRoute[0];
        }
    }
}
