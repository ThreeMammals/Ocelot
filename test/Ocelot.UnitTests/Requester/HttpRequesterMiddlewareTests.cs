namespace Ocelot.UnitTests.Requester
{
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.Configuration.Builder;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Requester;
    using Ocelot.Requester.Middleware;
    using Ocelot.Responses;
    using Ocelot.UnitTests.Responder;
    using Shouldly;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TestStack.BDDfy;
    using Xunit;

    public class HttpRequesterMiddlewareTests
    {
        private readonly Mock<IHttpRequester> _requester;
        private Response<HttpResponseMessage> _response;
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
            _next = context => Task.CompletedTask;
            _middleware = new HttpRequesterMiddleware(_next, _loggerFactory.Object, _requester.Object);
        }

        [Fact]
        public void should_call_services_correctly()
        {
            this.Given(x => x.GivenTheRequestIs())
                .And(x => x.GivenTheRequesterReturns(new OkResponse<HttpResponseMessage>(new HttpResponseMessage())))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamResponseIsSet())
                .BDDfy();
        }

        [Fact]
        public void should_set_error()
        {
            this.Given(x => x.GivenTheRequestIs())
                .And(x => x.GivenTheRequesterReturns(new ErrorResponse<HttpResponseMessage>(new AnyError())))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheErrorIsSet())
                .BDDfy();
        }

        private void ThenTheErrorIsSet()
        {
            _downstreamContext.IsError.ShouldBeTrue();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheRequestIs()
        {
            _downstreamContext =
                new DownstreamContext(new DefaultHttpContext())
                {
                    DownstreamReRoute = new DownstreamReRouteBuilder().Build()
                };
        }

        private void GivenTheRequesterReturns(Response<HttpResponseMessage> response)
        {
            _response = response;

            _requester
                .Setup(x => x.GetResponse(It.IsAny<DownstreamContext>()))
                .ReturnsAsync(_response);
        }

        private void ThenTheDownstreamResponseIsSet()
        {
            foreach (var httpResponseHeader in _response.Data.Headers)
            {
                if (_downstreamContext.DownstreamResponse.Headers.Any(x => x.Key == httpResponseHeader.Key))
                {
                    throw new Exception("Header in response not in downstreamresponse headers");
                }
            }

            _downstreamContext.DownstreamResponse.Content.ShouldBe(_response.Data.Content);
            _downstreamContext.DownstreamResponse.StatusCode.ShouldBe(_response.Data.StatusCode);
        }
    }
}
