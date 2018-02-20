using Ocelot.Middleware;

namespace Ocelot.UnitTests.Requester
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using System.Net.Http;
    using Moq;
    using Ocelot.Logging;
    using Ocelot.Requester;
    using Ocelot.Requester.Middleware;
    using Ocelot.Requester.QoS;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;
    using Shouldly;

    public class HttpRequesterMiddlewareTests
    {
        private readonly Mock<IHttpRequester> _requester;
        private OkResponse<HttpResponseMessage> _response;
        private OkResponse<Ocelot.Request.Request> _request;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly HttpRequesterMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;

        public HttpRequesterMiddlewareTests()
        {
            _requester = new Mock<IHttpRequester>();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<HttpRequesterMiddleware>()).Returns(_logger.Object);
            _next = async context => {
                //do nothing
            };
            _middleware = new HttpRequesterMiddleware(_next, _loggerFactory.Object, _requester.Object);
        }

        [Fact]
        public void should_call_scoped_data_repository_correctly()
        {
            this.Given(x => x.GivenTheRequestIs(new Ocelot.Request.Request(new HttpRequestMessage(),true, new NoQoSProvider(), false, false, "", false)))
                .And(x => x.GivenTheRequesterReturns(new HttpResponseMessage()))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedRepoIsCalledCorrectly())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }


        private void GivenTheRequestIs(Ocelot.Request.Request request)
        {
            _downstreamContext.Request = request;
        }

        private void GivenTheRequesterReturns(HttpResponseMessage response)
        {
            _response = new OkResponse<HttpResponseMessage>(response);
            _requester
                .Setup(x => x.GetResponse(It.IsAny<Ocelot.Request.Request>()))
                .ReturnsAsync(_response);
        }

        private void ThenTheScopedRepoIsCalledCorrectly()
        {
            _downstreamContext.DownstreamResponse.ShouldBe(_response.Data);
        }
    }
}
