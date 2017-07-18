namespace Ocelot.UnitTests.DownstreamUrlCreator
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.DownstreamUrlCreator;
    using Ocelot.DownstreamUrlCreator.Middleware;
    using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
    using Ocelot.Infrastructure.RequestData;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using Ocelot.Values;
    using TestStack.BDDfy;
    using Xunit;
    using Shouldly;

    public class DownstreamUrlCreatorMiddlewareTests : ServerHostedMiddlewareTest
    {
        private readonly Mock<IDownstreamPathPlaceholderReplacer> _downstreamUrlTemplateVariableReplacer;
        private readonly Mock<IUrlBuilder> _urlBuilder;
        private Response<DownstreamRoute> _downstreamRoute;
        private OkResponse<DownstreamPath> _downstreamPath;
        private HttpRequestMessage _downstreamRequest;

        public DownstreamUrlCreatorMiddlewareTests()
        {
            _downstreamUrlTemplateVariableReplacer = new Mock<IDownstreamPathPlaceholderReplacer>();
            _urlBuilder = new Mock<IUrlBuilder>();

            _downstreamRequest = new HttpRequestMessage(HttpMethod.Get, "https://my.url/abc/?q=123");

            ScopedRepository
                .Setup(sr => sr.Get<HttpRequestMessage>("DownstreamRequest"))
                .Returns(new OkResponse<HttpRequestMessage>(_downstreamRequest));

            GivenTheTestServerIsConfigured();
        }

        [Fact]
        public void should_replace_scheme_and_path()
        {
            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRoute(
                    new List<UrlPathPlaceholderNameAndValue>(), 
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("any old string")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithDownstreamScheme("https")
                        .Build())))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://my.url/abc?q=123"))
                .And(x => x.GivenTheUrlReplacerWillReturn("/api/products/1"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("https://my.url:80/api/products/1?q=123"))
                .BDDfy();
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(_downstreamUrlTemplateVariableReplacer.Object);
            services.AddSingleton(ScopedRepository.Object);
            services.AddSingleton(_urlBuilder.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseDownstreamUrlCreatorMiddleware();
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            ScopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void GivenTheDownstreamRequestUriIs(string uri)
        {
            _downstreamRequest.RequestUri = new Uri(uri);
        }

        private void GivenTheUrlReplacerWillReturn(string path)
        {
            _downstreamPath = new OkResponse<DownstreamPath>(new DownstreamPath(path));
            _downstreamUrlTemplateVariableReplacer
                .Setup(x => x.Replace(It.IsAny<PathTemplate>(), It.IsAny<List<UrlPathPlaceholderNameAndValue>>()))
                .Returns(_downstreamPath);
        }

        private void ThenTheDownstreamRequestUriIs(string expectedUri)
        {
            _downstreamRequest.RequestUri.OriginalString.ShouldBe(expectedUri);
        }
    }
}
