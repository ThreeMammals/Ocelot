namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.Configuration.Provider;
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
        private readonly Mock<IOcelotConfigurationProvider> _provider;
        private Response<DownstreamRoute> _downstreamRoute;
        private IOcelotConfiguration _config;

        public DownstreamRouteFinderMiddlewareTests()
        {
            _provider = new Mock<IOcelotConfigurationProvider>();
            _downstreamRouteFinder = new Mock<IDownstreamRouteFinder>();

            GivenTheTestServerIsConfigured();
        }

        [Fact]
        public void should_call_scoped_data_repository_correctly()
        {
            var config = new OcelotConfiguration(null, null, new ServiceProviderConfigurationBuilder().Build(), "");

            this.Given(x => x.GivenTheDownStreamRouteFinderReturns(
                new DownstreamRoute(
                    new List<PlaceholderNameAndValue>(), 
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("any old string")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .Build())))
                .And(x => GivenTheFollowingConfig(config))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheFollowingConfig(IOcelotConfiguration config)
        {
            _config = config;
            _provider
                .Setup(x => x.Get())
                .ReturnsAsync(new OkResponse<IOcelotConfiguration>(_config));
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(_downstreamRouteFinder.Object);
            services.AddSingleton(_provider.Object);
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
                .Setup(x => x.FindDownstreamRoute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IOcelotConfiguration>(), It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            ScopedRepository
                .Verify(x => x.Add("DownstreamRoute", _downstreamRoute.Data), Times.Once());

            ScopedRepository
                .Verify(x => x.Add("ServiceProviderConfiguration", _config.ServiceProviderConfiguration), Times.Once());
        }
    }
}
