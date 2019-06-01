namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.Finder;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Middleware.Multiplexer;
    using Ocelot.Responses;
    using Shouldly;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using TestStack.BDDfy;
    using Xunit;

    public class DownstreamRouteFinderMiddlewareTests
    {
        private readonly Mock<IDownstreamRouteProvider> _finder;
        private readonly Mock<IDownstreamRouteProviderFactory> _factory;
        private Response<DownstreamRoute> _downstreamRoute;
        private IInternalConfiguration _config;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly DownstreamRouteFinderMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;
        private readonly Mock<IMultiplexer> _multiplexer;

        public DownstreamRouteFinderMiddlewareTests()
        {
            _finder = new Mock<IDownstreamRouteProvider>();
            _factory = new Mock<IDownstreamRouteProviderFactory>();
            _factory.Setup(x => x.Get(It.IsAny<IInternalConfiguration>())).Returns(_finder.Object);
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<DownstreamRouteFinderMiddleware>()).Returns(_logger.Object);
            _next = context => Task.CompletedTask;
            _multiplexer = new Mock<IMultiplexer>();
            _middleware = new DownstreamRouteFinderMiddleware(_next, _loggerFactory.Object, _factory.Object, _multiplexer.Object);
        }

        [Fact]
        public void should_call_scoped_data_repository_correctly()
        {
            var config = new InternalConfiguration(null, null, new ServiceProviderConfigurationBuilder().Build(), "", new LoadBalancerOptionsBuilder().Build(), "", new QoSOptionsBuilder().Build(), new HttpHandlerOptionsBuilder().Build());

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamPathTemplate("any old string")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteFinderReturns(
                new DownstreamRoute(
                    new List<PlaceholderNameAndValue>(),
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(downstreamReRoute)
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .Build())))
                .And(x => GivenTheFollowingConfig(config))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetType();
        }

        private void GivenTheFollowingConfig(IInternalConfiguration config)
        {
            _config = config;
            _downstreamContext.Configuration = config;
        }

        private void GivenTheDownStreamRouteFinderReturns(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _finder
                .Setup(x => x.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IInternalConfiguration>(), It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            _downstreamContext.TemplatePlaceholderNameAndValues.ShouldBe(_downstreamRoute.Data.TemplatePlaceholderNameAndValues);
            _downstreamContext.Configuration.ServiceProviderConfiguration.ShouldBe(_config.ServiceProviderConfiguration);
        }
    }
}
