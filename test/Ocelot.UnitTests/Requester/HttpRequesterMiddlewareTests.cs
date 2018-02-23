using Ocelot.Configuration.Builder;
using Ocelot.Middleware;

namespace Ocelot.UnitTests.Requester
{
    using Microsoft.AspNetCore.Http;
    using System.Net.Http;
    using Moq;
    using Ocelot.Logging;
    using Ocelot.Requester;
    using Ocelot.Requester.Middleware;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;
    using Shouldly;

    public class HttpRequesterMiddlewareTests
    {
        private readonly Mock<IHttpRequester> _requester;
        private OkResponse<HttpResponseMessage> _response;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly HttpRequesterMiddleware _middleware;
        private DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;

        public HttpRequesterMiddlewareTests()
        {
            _requester = new Mock<IHttpRequester>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<HttpRequesterMiddleware>()).Returns(_logger.Object);
            _next = async context => {
                //do nothing
            };
            _middleware = new HttpRequesterMiddleware(_next, _loggerFactory.Object, _requester.Object);
        }

        [Fact]
        public void should_call_services_correctly()
        {
            this.Given(x => x.GivenTheRequestIs())
                .And(x => x.GivenTheRequesterReturns(new HttpResponseMessage()))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedRepoIsCalledCorrectly())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheRequestIs()
        {
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _downstreamContext.DownstreamReRoute = new DownstreamReRouteBuilder().Build();
        }

        private void GivenTheRequesterReturns(HttpResponseMessage response)
        {
            _response = new OkResponse<HttpResponseMessage>(response);
            _requester
                .Setup(x => x.GetResponse(It.IsAny<DownstreamContext>()))
                .ReturnsAsync(_response);
        }

        private void ThenTheScopedRepoIsCalledCorrectly()
        {
            _downstreamContext.DownstreamResponse.ShouldBe(_response.Data);
        }
    }
}
