using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.Middleware;
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
        private HttpResponseMessage _result;
        private OkResponse<DownstreamPath> _downstreamPath;
        private OkResponse<DownstreamUrl> _downstreamUrl;
        private HostAndPort _hostAndPort;

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

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void should_call_dependencies_correctly()
        {
            var hostAndPort = new HostAndPort("127.0.0.1", 80);

            this.Given(x => x.GivenTheDownStreamRouteIs(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(), new ReRouteBuilder().WithDownstreamPathTemplate("any old string").Build())))
                .And(x => x.GivenTheHostAndPortIs(hostAndPort))
                .And(x => x.TheUrlReplacerReturns("/api/products/1"))
                .And(x => x.TheUrlBuilderReturns("http://127.0.0.1:80/api/products/1"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheHostAndPortIs(HostAndPort hostAndPort)
        {
            _hostAndPort = hostAndPort;
            _scopedRepository
                .Setup(x => x.Get<HostAndPort>("HostAndPort"))
                .Returns(new OkResponse<HostAndPort>(_hostAndPort));
        }

        private void TheUrlBuilderReturns(string dsUrl)
        {
            _downstreamUrl = new OkResponse<DownstreamUrl>(new DownstreamUrl(dsUrl));
            _urlBuilder
                .Setup(x => x.Build(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HostAndPort>()))
                .Returns(_downstreamUrl);
        }

        private void TheUrlReplacerReturns(string downstreamUrl)
        {
            _downstreamPath = new OkResponse<DownstreamPath>(new DownstreamPath(downstreamUrl));
            _downstreamUrlTemplateVariableReplacer
                .Setup(x => x.Replace(It.IsAny<DownstreamPathTemplate>(), It.IsAny<List<UrlPathPlaceholderNameAndValue>>()))
                .Returns(_downstreamPath);
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            _scopedRepository
                .Verify(x => x.Add("DownstreamUrl", _downstreamUrl.Data.Value), Times.Once());
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _scopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}
