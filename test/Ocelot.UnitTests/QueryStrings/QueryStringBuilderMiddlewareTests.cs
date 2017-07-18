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

    public class QueryStringBuilderMiddlewareTests : ServerHostedMiddlewareTest
    {
        private readonly Mock<IAddQueriesToRequest> _addQueries;
        private readonly HttpRequestMessage _downstreamRequest;
        private Response<DownstreamRoute> _downstreamRoute;

        public QueryStringBuilderMiddlewareTests()
        {
            _addQueries = new Mock<IAddQueriesToRequest>();

            _downstreamRequest = new HttpRequestMessage();
            ScopedRepository.Setup(sr => sr.Get<HttpRequestMessage>("DownstreamRequest"))
                .Returns(new OkResponse<HttpRequestMessage>(_downstreamRequest));

            GivenTheTestServerIsConfigured();
        }

        [Fact]
        public void should_call_add_queries_correctly()
        {
            var downstreamRoute = new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("any old string")
                    .WithClaimsToQueries(new List<ClaimToThing>
                    {
                        new ClaimToThing("UserId", "Subject", "", 0)
                    })
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheAddHeadersToRequestReturnsOk())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheAddQueriesToRequestIsCalledCorrectly())
                .BDDfy();
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(_addQueries.Object);
            services.AddSingleton(ScopedRepository.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseQueryStringBuilderMiddleware();
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
                    _downstreamRequest), Times.Once);
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            ScopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }
    }
}
