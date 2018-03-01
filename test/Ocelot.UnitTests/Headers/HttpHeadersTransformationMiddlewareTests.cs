using Xunit;
using Ocelot.Logging;
using Ocelot.Headers.Middleware;
using TestStack.BDDfy;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Configuration.Builder;
using Ocelot.Headers;
using System.Net.Http;
using Ocelot.Authorisation.Middleware;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.Middleware;

namespace Ocelot.UnitTests.Headers
{
    public class HttpHeadersTransformationMiddlewareTests
    {
        private Mock<IHttpContextRequestHeaderReplacer> _preReplacer;
        private Mock<IHttpResponseHeaderReplacer> _postReplacer;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private HttpHeadersTransformationMiddleware _middleware;
        private DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;

        public HttpHeadersTransformationMiddlewareTests()
        {
            _preReplacer = new Mock<IHttpContextRequestHeaderReplacer>();
            _postReplacer = new Mock<IHttpResponseHeaderReplacer>();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<AuthorisationMiddleware>()).Returns(_logger.Object);
            _next = async context => {
                //do nothing
            };
            _middleware = new HttpHeadersTransformationMiddleware(_next, _loggerFactory.Object, _preReplacer.Object, _postReplacer.Object);
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
                .And(x => ThenTheIHttpResponseHeaderReplacerIsCalledCorrectly())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheDownstreamRequestIs()
        {
            _downstreamContext.DownstreamRequest = new HttpRequestMessage();
        }

        private void GivenTheHttpResponseMessageIs()
        {
            _downstreamContext.DownstreamResponse = new HttpResponseMessage();
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
            _postReplacer.Verify(x => x.Replace(It.IsAny<HttpResponseMessage>(), It.IsAny<List<HeaderFindAndReplace>>(), It.IsAny<HttpRequestMessage>()), Times.Once);
        }

        private void GivenTheFollowingRequest()
        {
            _downstreamContext.HttpContext.Request.Headers.Add("test", "test");
        }
    }
}
