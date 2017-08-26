namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.Finder;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;

    public class DownstreamRouteFinderMiddlewareTests : ServerHostedMiddlewareTest
    {
        private readonly Mock<IDownstreamRouteFinder> _downstreamRouteFinder;
        private Response<DownstreamRoute> _downstreamRoute;

        public DownstreamRouteFinderMiddlewareTests()
        {
            _downstreamRouteFinder = new Mock<IDownstreamRouteFinder>();

            GivenTheTestServerIsConfigured();
        }

        [Fact]
        public void should_call_scoped_data_repository_correctly()
        {
            this.Given(x => x.GivenTheDownStreamRouteFinderReturns(
                new DownstreamRoute(
                    new List<UrlPathPlaceholderNameAndValue>(), 
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("any old string")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .Build())))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(_downstreamRouteFinder.Object);
            services.AddSingleton(ScopedRepository.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseDownstreamRouteFinderMiddleware();
        }

        private void GivenTheDownStreamRouteFinderReturns(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _downstreamRouteFinder
                .Setup(x => x.FindDownstreamRoute(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(_downstreamRoute);
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            ScopedRepository
                .Verify(x => x.Add("DownstreamRoute", _downstreamRoute.Data), Times.Once());
        }
    }
}
