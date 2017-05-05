using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
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

namespace Ocelot.UnitTests.DownstreamUrlCreator
{
    public class DownstreamUrlCreatorMiddlewareTests : IDisposable
    {
        private readonly Mock<IDownstreamPathPlaceholderReplacer> _downstreamUrlTemplateVariableReplacer;
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly Mock<IUrlBuilder> _urlBuilder;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private Response<DownstreamRoute> _downstreamRoute;
        private OkResponse<DownstreamPath> _downstreamPath;
        private HttpRequestMessage _downstreamRequest;
        private HttpResponseMessage _result;

        public DownstreamUrlCreatorMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _downstreamUrlTemplateVariableReplacer = new Mock<IDownstreamPathPlaceholderReplacer>();
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
            _urlBuilder = new Mock<IUrlBuilder>();
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
                  x.AddLogging();
                  x.AddSingleton(_downstreamUrlTemplateVariableReplacer.Object);
                  x.AddSingleton(_scopedRepository.Object);
                  x.AddSingleton(_urlBuilder.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseDownstreamUrlCreatorMiddleware();
              });

            _downstreamRequest = new HttpRequestMessage(HttpMethod.Get, "https://my.url/abc/?q=123");

            _scopedRepository
                .Setup(sr => sr.Get<HttpRequestMessage>("DownstreamRequest"))
                .Returns(new OkResponse<HttpRequestMessage>(_downstreamRequest));

            _server = new TestServer(builder);
            _client = _server.CreateClient();
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

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _scopedRepository
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

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void ThenTheDownstreamRequestUriIs(string expectedUri)
        {
            _downstreamRequest.RequestUri.OriginalString.ShouldBe(expectedUri);
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}
