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

namespace Ocelot.UnitTests.Headers
{
    public class HttpHeadersTransformationMiddlewareTests : ServerHostedMiddlewareTest
    {
        private Mock<IHttpContextRequestHeaderReplacer> _replacer;
        
        public HttpHeadersTransformationMiddlewareTests()
        {
            _replacer = new Mock<IHttpContextRequestHeaderReplacer>();
            GivenTheTestServerIsConfigured();
        }

        [Fact]
        public void should_do_nothing()
        {
            this.Given(x => GivenTheFollowingRequestWillNotBeTransformed())
                .And(x => GivenTheReRouteHasFindAndReplaceSetUp())
                .When(x => WhenICallTheMiddleware())
                .Then(x => ThenTheHeaderIsNotReturned())
                .BDDfy();
        }

        [Fact]
        public void should_call_pre_request_header_transform()
        {
            this.Given(x => GivenTheFollowingRequest())
                .And(x => GivenTheReRouteHasFindAndReplaceSetUp())
                .When(x => WhenICallTheMiddleware())
                .Then(x => ThenTheIHttpContextRequestHeaderReplacerIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_find_header_after_next_middleware_and_transform()
        {

        }

        private void GivenTheReRouteHasFindAndReplaceSetUp()
        {
            var fAndRs = new List<HeaderFindAndReplace>();
            fAndRs.Add(new HeaderFindAndReplace("test", "test", "chicken", 0));
            var reRoute = new ReRouteBuilder().WithUpstreamHeaderFindAndReplace(fAndRs).Build();
            var dR = new DownstreamRoute(null, reRoute);
            var response = new OkResponse<DownstreamRoute>(dR);
            ScopedRepository.Setup(x => x.Get<DownstreamRoute>("DownstreamRoute")).Returns(response);
        }

        private void ThenTheIHttpContextRequestHeaderReplacerIsCalledCorrectly()
        {
            _replacer.Verify(x => x.Replace(It.IsAny<HttpContext>(), It.IsAny<List<HeaderFindAndReplace>>()), Times.Once);
        }

        private void GivenTheFollowingRequest()
        {
            Client.DefaultRequestHeaders.Add("test", "test");
        }

        private void GivenTheFollowingRequestWillNotBeTransformed()
        {
            Client.DefaultRequestHeaders.Add("boop", "boop");
        }

        private void ThenTheHeaderIsNotReturned()
        {
            ResponseMessage.Headers.TryGetValues("boop", out var test).ShouldBeFalse();
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(ScopedRepository.Object);
            services.AddSingleton(_replacer.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseHttpHeadersTransformationMiddleware();
        }
    }
}