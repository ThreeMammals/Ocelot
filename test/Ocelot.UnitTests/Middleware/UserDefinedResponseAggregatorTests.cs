using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests.Middleware
{
    public class UserDefinedResponseAggregatorTests
    {
        private readonly UserDefinedResponseAggregator _aggregator;
        private readonly Mock<IDefinedAggregatorProvider> _provider;

        // todo - refactor to bddfy
        public UserDefinedResponseAggregatorTests()
        {
            _provider = new Mock<IDefinedAggregatorProvider>();
            _aggregator = new UserDefinedResponseAggregator(_provider.Object);
        }

        [Fact]
        public async Task should_call_aggregator()
        {
            var aggregator = new TestDefinedAggregator();
            _provider.Setup(x => x.Get(It.IsAny<ReRoute>())).Returns(new OkResponse<IDefinedAggregator>(aggregator));
            var reRoute = new ReRouteBuilder().Build();
            var context = new DownstreamContext(new DefaultHttpContext());
            var contexts = new List<DownstreamContext>
            {
                new DownstreamContext(new DefaultHttpContext())
                {
                    DownstreamResponse = new DownstreamResponse(new StringContent("Tom"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>())
                },
                new DownstreamContext(new DefaultHttpContext())
                {
                    DownstreamResponse = new DownstreamResponse(new StringContent("Laura"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>())
                }
            };

            await _aggregator.Aggregate(reRoute, context, contexts);
            _provider.Verify(x => x.Get(reRoute), Times.Once);
            var content = await context.DownstreamResponse.Content.ReadAsStringAsync();
            content.ShouldBe("Tom, Laura");
        }

        [Fact]
        public async Task should_not_find_aggregator()
        {
            _provider.Setup(x => x.Get(It.IsAny<ReRoute>())).Returns(new ErrorResponse<IDefinedAggregator>(new AnyError()));
            var reRoute = new ReRouteBuilder().Build();
            var context = new DownstreamContext(new DefaultHttpContext());
            var contexts = new List<DownstreamContext>
            {
                new DownstreamContext(new DefaultHttpContext())
                { 
                    DownstreamResponse = new DownstreamResponse(new StringContent("Tom"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>())

                },
                new DownstreamContext(new DefaultHttpContext())
                {
                    DownstreamResponse = new DownstreamResponse(new StringContent("Laura"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>())
                }
            };

            await _aggregator.Aggregate(reRoute, context, contexts);
            _provider.Verify(x => x.Get(reRoute), Times.Once);
            context.IsError.ShouldBeTrue();
            context.Errors.Count.ShouldBe(1);
        }

        public class TestDefinedAggregator : IDefinedAggregator
        {
            public async Task<DownstreamResponse> Aggregate(List<DownstreamResponse> responses)
            {
                var tom = await responses[0].Content.ReadAsStringAsync();
                var laura = await responses[1].Content.ReadAsStringAsync();
                var content = $"{tom}, {laura}";
                var headers = responses.SelectMany(x => x.Headers).ToList();
                return new DownstreamResponse(new StringContent(content), HttpStatusCode.OK, headers);
            }
        }
    }
}
