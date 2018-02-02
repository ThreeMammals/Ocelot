using Xunit;
using Shouldly;
using Ocelot.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Ocelot.Headers.Middleware;
using TestStack.BDDfy;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Responses;
using Ocelot.Configuration.Builder;
using Ocelot.Headers;
using System.Net.Http;

namespace Ocelot.UnitTests.Headers
{
    public class HttpHeadersTransformationMiddlewareTests : ServerHostedMiddlewareTest
    {
        private Mock<IHttpContextRequestHeaderReplacer> _preReplacer;
        private Mock<IHttpResponseHeaderReplacer> _postReplacer;
        
        public HttpHeadersTransformationMiddlewareTests()
        {
            _preReplacer = new Mock<IHttpContextRequestHeaderReplacer>();
            _postReplacer = new Mock<IHttpResponseHeaderReplacer>();
            
            GivenTheTestServerIsConfigured();
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

        private void GivenTheDownstreamRequestIs()
        {
            var request = new HttpRequestMessage();
            var response = new OkResponse<HttpRequestMessage>(request);
            ScopedRepository.Setup(x => x.Get<HttpRequestMessage>("DownstreamRequest")).Returns(response);
        }

        private void GivenTheHttpResponseMessageIs()
        {
            var httpResponseMessage = new HttpResponseMessage();
            var response = new OkResponse<HttpResponseMessage>(httpResponseMessage);
            ScopedRepository.Setup(x => x.Get<HttpResponseMessage>("HttpResponseMessage")).Returns(response);
        }

        private void GivenTheReRouteHasPreFindAndReplaceSetUp()
        {
            var fAndRs = new List<HeaderFindAndReplace>();
            var reRoute = new ReRouteBuilder().WithUpstreamHeaderFindAndReplace(fAndRs).WithDownstreamHeaderFindAndReplace(fAndRs).Build();
            var dR = new DownstreamRoute(null, reRoute);
            var response = new OkResponse<DownstreamRoute>(dR);
            ScopedRepository.Setup(x => x.Get<DownstreamRoute>("DownstreamRoute")).Returns(response);
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
            Client.DefaultRequestHeaders.Add("test", "test");
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(ScopedRepository.Object);
            services.AddSingleton(_preReplacer.Object);
            services.AddSingleton(_postReplacer.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseHttpHeadersTransformationMiddleware();
        }
    }
}