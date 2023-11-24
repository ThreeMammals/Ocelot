using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Multiplexer;

namespace Ocelot.UnitTests.Multiplexing
{
    public class MultiplexingMiddlewareTests
    {
        private readonly MultiplexingMiddleware _middleware;
        private Ocelot.DownstreamRouteFinder.DownstreamRouteHolder _downstreamRoute;
        private int _count;
        private readonly HttpContext _httpContext;

        public MultiplexingMiddlewareTests()
        {
            _httpContext = new DefaultHttpContext();
            var factory = new Mock<IResponseAggregatorFactory>();
            var aggregator = new Mock<IResponseAggregator>();
            factory.Setup(x => x.Get(It.IsAny<Route>())).Returns(aggregator.Object);
            var loggerFactory = new Mock<IOcelotLoggerFactory>();
            var logger = new Mock<IOcelotLogger>();
            loggerFactory.Setup(x => x.CreateLogger<MultiplexingMiddleware>()).Returns(logger.Object);
            Task Next(HttpContext context) => Task.FromResult(_count++);
            _middleware = new MultiplexingMiddleware(Next, loggerFactory.Object, factory.Object);
        }

        [Fact]
        public void should_multiplex()
        {
            var route = new RouteBuilder().WithDownstreamRoute(new DownstreamRouteBuilder().Build()).WithDownstreamRoute(new DownstreamRouteBuilder().Build()).Build();

            this.Given(x => GivenTheFollowing(route))
                .When(x => WhenIMultiplex())
                .Then(x => ThePipelineIsCalled(2))
                .BDDfy();
        }

        [Fact]
        public void should_not_multiplex()
        {
            var route = new RouteBuilder().WithDownstreamRoute(new DownstreamRouteBuilder().Build()).Build();

            this.Given(x => GivenTheFollowing(route))
                .When(x => WhenIMultiplex())
                .Then(x => ThePipelineIsCalled(1))
                .BDDfy();
        }

        private void GivenTheFollowing(Route route)
        {
            _downstreamRoute = new Ocelot.DownstreamRouteFinder.DownstreamRouteHolder(new List<PlaceholderNameAndValue>(), route);
            _httpContext.Items.UpsertDownstreamRoute(_downstreamRoute);
        }

        private void WhenIMultiplex()
        {
            _middleware.Invoke(_httpContext).GetAwaiter().GetResult();
        }

        private void ThePipelineIsCalled(int expected)
        {
            _count.ShouldBe(expected);
        }
    }
}
