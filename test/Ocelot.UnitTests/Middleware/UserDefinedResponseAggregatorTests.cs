using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Middleware
{
    public class UserDefinedResponseAggregatorTests
    {
        private readonly UserDefinedResponseAggregator _aggregator;
        private readonly Mock<IDefinedAggregatorProvider> _provider;
        private ReRoute _reRoute;
        private List<DownstreamContext> _contexts;
        private DownstreamContext _context;

        public UserDefinedResponseAggregatorTests()
        {
            _provider = new Mock<IDefinedAggregatorProvider>();
            _aggregator = new UserDefinedResponseAggregator(_provider.Object);
        }

        [Fact]
        public void should_call_aggregator()
        {
            var reRoute = new ReRouteBuilder().Build();

            var context = new DownstreamContext(new DefaultHttpContext());

            var contexts = new List<DownstreamContext>
            {
                new DownstreamContext(new DefaultHttpContext())
                {
                    DownstreamResponse = new DownstreamResponse(new StringContent("Tom"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason")
                },
                new DownstreamContext(new DefaultHttpContext())
                {
                    DownstreamResponse = new DownstreamResponse(new StringContent("Laura"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason")
                }
            };

            this.Given(_ => GivenTheProviderReturnsAggregator())
                .And(_ => GivenReRoute(reRoute))
                .And(_ => GivenContexts(contexts))
                .And(_ => GivenContext(context))
                .When(_ => WhenIAggregate())
                .Then(_ => ThenTheProviderIsCalled())
                .And(_ => ThenTheContentIsCorrect())
                .BDDfy();
        }

        [Fact]
        public void should_not_find_aggregator()
        {
            var reRoute = new ReRouteBuilder().Build();

            var context = new DownstreamContext(new DefaultHttpContext());

            var contexts = new List<DownstreamContext>
            {
                new DownstreamContext(new DefaultHttpContext())
                {
                    DownstreamResponse = new DownstreamResponse(new StringContent("Tom"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason")
                },
                new DownstreamContext(new DefaultHttpContext())
                {
                    DownstreamResponse = new DownstreamResponse(new StringContent("Laura"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason")
                }
            };

            this.Given(_ => GivenTheProviderReturnsError())
                .And(_ => GivenReRoute(reRoute))
                .And(_ => GivenContexts(contexts))
                .And(_ => GivenContext(context))
                .When(_ => WhenIAggregate())
                .Then(_ => ThenTheProviderIsCalled())
                .And(_ => ThenTheErrorIsReturned())
                .BDDfy();
        }

        private void ThenTheErrorIsReturned()
        {
            _context.IsError.ShouldBeTrue();
            _context.Errors.Count.ShouldBe(1);
        }

        private void GivenTheProviderReturnsError()
        {
            _provider.Setup(x => x.Get(It.IsAny<ReRoute>())).Returns(new ErrorResponse<IDefinedAggregator>(new AnyError()));
        }

        private async Task ThenTheContentIsCorrect()
        {
            var content = await _context.DownstreamResponse.Content.ReadAsStringAsync();
            content.ShouldBe("Tom, Laura");
        }

        private void ThenTheProviderIsCalled()
        {
            _provider.Verify(x => x.Get(_reRoute), Times.Once);
        }

        private void GivenContext(DownstreamContext context)
        {
            _context = context;
        }

        private void GivenContexts(List<DownstreamContext> contexts)
        {
            _contexts = contexts;
        }

        private async Task WhenIAggregate()
        {
            await _aggregator.Aggregate(_reRoute, _context, _contexts);
        }

        private void GivenTheProviderReturnsAggregator()
        {
            var aggregator = new TestDefinedAggregator();
            _provider.Setup(x => x.Get(It.IsAny<ReRoute>())).Returns(new OkResponse<IDefinedAggregator>(aggregator));
        }

        private void GivenReRoute(ReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        public class TestDefinedAggregator : IDefinedAggregator
        {
            public async Task<DownstreamResponse> Aggregate(List<DownstreamContext> responses)
            {
                var tom = await responses[0].DownstreamResponse.Content.ReadAsStringAsync();
                var laura = await responses[1].DownstreamResponse.Content.ReadAsStringAsync();
                var content = $"{tom}, {laura}";
                var headers = responses.SelectMany(x => x.DownstreamResponse.Headers).ToList();
                return new DownstreamResponse(new StringContent(content), HttpStatusCode.OK, headers, "some reason");
            }
        }
    }
}
