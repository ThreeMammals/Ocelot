using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;
using Shouldly;
using System.Threading.Tasks;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Middleware
{
    public class MultiplexerTests
    {
        private readonly Multiplexer _multiplexer;
        private readonly DownstreamContext _context;
        private ReRoute _reRoute;
        private readonly OcelotRequestDelegate _pipeline;
        private int _count;
        private Mock<IResponseAggregator> _aggregator;
        private Mock<IResponseAggregatorFactory> _factory;

        public MultiplexerTests()
        {
            _factory = new Mock<IResponseAggregatorFactory>();
            _aggregator = new Mock<IResponseAggregator>();
            _context = new DownstreamContext(new DefaultHttpContext());
            _pipeline = context => Task.FromResult(_count++);
            _factory.Setup(x => x.Get(It.IsAny<ReRoute>())).Returns(_aggregator.Object);
            _multiplexer = new Multiplexer(_factory.Object);
        }

        [Fact]
        public void should_multiplex()
        {
            var reRoute = new ReRouteBuilder().WithDownstreamReRoute(new DownstreamReRouteBuilder().Build()).WithDownstreamReRoute(new DownstreamReRouteBuilder().Build()).Build();

            this.Given(x => GivenTheFollowing(reRoute))
                .When(x => WhenIMultiplex())
                .Then(x => ThePipelineIsCalled(2))
                .BDDfy();
        }

        [Fact]
        public void should_not_multiplex()
        {
            var reRoute = new ReRouteBuilder().WithDownstreamReRoute(new DownstreamReRouteBuilder().Build()).Build();

            this.Given(x => GivenTheFollowing(reRoute))
                .When(x => WhenIMultiplex())
                .Then(x => ThePipelineIsCalled(1))
                .BDDfy();
        }

        private void GivenTheFollowing(ReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenIMultiplex()
        {
            _multiplexer.Multiplex(_context, _reRoute, _pipeline).GetAwaiter().GetResult();
        }

        private void ThePipelineIsCalled(int expected)
        {
            _count.ShouldBe(expected);
        }
    }
}
