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
        private UserDefinedResponseAggregator _aggregator;
        private Mock<IDefinedAggregatorProvider> _provider;

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
                    DownstreamResponse = new HttpResponseMessage
                    {
                        Content = new StringContent("Tom")
                    }
                },
                new DownstreamContext(new DefaultHttpContext())
                { 
                    DownstreamResponse = new HttpResponseMessage
                    {
                        Content = new StringContent("Laura")
                    }
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
            var aggregator = new TestDefinedAggregator();
            _provider.Setup(x => x.Get(It.IsAny<ReRoute>())).Returns(new ErrorResponse<IDefinedAggregator>(new AnyError()));
            var reRoute = new ReRouteBuilder().Build();
            var context = new DownstreamContext(new DefaultHttpContext());
            var contexts = new List<DownstreamContext>
            {
                new DownstreamContext(new DefaultHttpContext())
                { 
                    DownstreamResponse = new HttpResponseMessage
                    {
                        Content = new StringContent("Tom")
                    }
                },
                new DownstreamContext(new DefaultHttpContext())
                { 
                    DownstreamResponse = new HttpResponseMessage
                    {
                        Content = new StringContent("Laura")
                    }
                }
            };

            await _aggregator.Aggregate(reRoute, context, contexts);
            _provider.Verify(x => x.Get(reRoute), Times.Once);
            context.IsError.ShouldBeTrue();
            context.Errors.Count.ShouldBe(1);
        }

        public class TestDefinedAggregator : IDefinedAggregator
        {
            public async Task<AggregateResponse> Aggregate(List<HttpResponseMessage> responses)
            {
                var tom = await responses[0].Content.ReadAsStringAsync();
                var laura = await responses[1].Content.ReadAsStringAsync();
                var content = $"{tom}, {laura}";
                var headers = responses.SelectMany(x => x.Headers).ToList();
                return new AggregateResponse(new StringContent(content), HttpStatusCode.OK, headers);
            }
        }
    }
}