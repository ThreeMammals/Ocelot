namespace Ocelot.UnitTests.Headers
{
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.Authorisation.Middleware;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.Headers;
    using Ocelot.Headers.Middleware;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Request.Middleware;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TestStack.BDDfy;
    using Xunit;

    public class HttpHeadersTransformationMiddlewareTests
    {
        private readonly Mock<IHttpContextRequestHeaderReplacer> _preReplacer;
        private readonly Mock<IHttpResponseHeaderReplacer> _postReplacer;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly HttpHeadersTransformationMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;
        private readonly Mock<IAddHeadersToResponse> _addHeadersToResponse;
        private readonly Mock<IAddHeadersToRequest> _addHeadersToRequest;

        public HttpHeadersTransformationMiddlewareTests()
        {
            _preReplacer = new Mock<IHttpContextRequestHeaderReplacer>();
            _postReplacer = new Mock<IHttpResponseHeaderReplacer>();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<AuthorisationMiddleware>()).Returns(_logger.Object);
            _next = context => Task.CompletedTask;
            _addHeadersToResponse = new Mock<IAddHeadersToResponse>();
            _addHeadersToRequest = new Mock<IAddHeadersToRequest>();
            _middleware = new HttpHeadersTransformationMiddleware(
                _next, _loggerFactory.Object, _preReplacer.Object,
                _postReplacer.Object, _addHeadersToResponse.Object, _addHeadersToRequest.Object);
        }

        [Fact]
        public void should_call_pre_and_post_header_transforms()
        {
            this.Given(x => GivenTheFollowingRequest())
                .And(x => GivenTheDownstreamRequestIs())
                .And(x => GivenTheReRouteHasPreFindAndReplaceSetUp())
                .And(x => GivenTheHttpResponseMessageIs())
                .When(x => WhenICallTheMiddleware())
                .Then(x => ThenTheIHttpContextRequestHeaderReplacerIsCalledCorrectly())
                .Then(x => ThenAddHeadersToRequestIsCalledCorrectly())
                .And(x => ThenTheIHttpResponseHeaderReplacerIsCalledCorrectly())
                .And(x => ThenAddHeadersToResponseIsCalledCorrectly())
                .BDDfy();
        }

        private void ThenAddHeadersToResponseIsCalledCorrectly()
        {
            _addHeadersToResponse
                .Verify(x => x.Add(_downstreamContext.DownstreamReRoute.AddHeadersToDownstream, _downstreamContext.DownstreamResponse), Times.Once);
        }

        private void ThenAddHeadersToRequestIsCalledCorrectly()
        {
            _addHeadersToRequest
                .Verify(x => x.SetHeadersOnDownstreamRequest(_downstreamContext.DownstreamReRoute.AddHeadersToUpstream, _downstreamContext.HttpContext), Times.Once);
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheDownstreamRequestIs()
        {
            _downstreamContext.DownstreamRequest = new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com"));
        }

        private void GivenTheHttpResponseMessageIs()
        {
            _downstreamContext.DownstreamResponse = new DownstreamResponse(new HttpResponseMessage());
        }

        private void GivenTheReRouteHasPreFindAndReplaceSetUp()
        {
            var fAndRs = new List<HeaderFindAndReplace>();
            var reRoute = new ReRouteBuilder()
                .WithDownstreamReRoute(new DownstreamReRouteBuilder().WithUpstreamHeaderFindAndReplace(fAndRs)
                    .WithDownstreamHeaderFindAndReplace(fAndRs).Build())
                .Build();

            var dR = new DownstreamRoute(null, reRoute);

            _downstreamContext.TemplatePlaceholderNameAndValues = dR.TemplatePlaceholderNameAndValues;
            _downstreamContext.DownstreamReRoute = dR.ReRoute.DownstreamReRoute[0];
        }

        private void ThenTheIHttpContextRequestHeaderReplacerIsCalledCorrectly()
        {
            _preReplacer.Verify(x => x.Replace(It.IsAny<HttpContext>(), It.IsAny<List<HeaderFindAndReplace>>()), Times.Once);
        }

        private void ThenTheIHttpResponseHeaderReplacerIsCalledCorrectly()
        {
            _postReplacer.Verify(x => x.Replace(It.IsAny<DownstreamContext>(), It.IsAny<List<HeaderFindAndReplace>>()), Times.Once);
        }

        private void GivenTheFollowingRequest()
        {
            _downstreamContext.HttpContext.Request.Headers.Add("test", "test");
        }
    }
}
