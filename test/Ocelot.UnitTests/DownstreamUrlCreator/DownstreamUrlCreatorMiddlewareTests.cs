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
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamUrlCreator
{
    public class DownstreamUrlCreatorMiddlewareTests : IDisposable
    {
        private readonly Mock<IDownstreamUrlPathPlaceholderReplacer> _downstreamUrlTemplateVariableReplacer;
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private Response<DownstreamRoute> _downstreamRoute;
        private HttpResponseMessage _result;
        private OkResponse<DownstreamUrl> _downstreamUrl;
        private Mock<IOcelotLoggerFactory> _mockLoggerFactory;

        public DownstreamUrlCreatorMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _downstreamUrlTemplateVariableReplacer = new Mock<IDownstreamUrlPathPlaceholderReplacer>();
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
            SetUpLogger();

            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton(_mockLoggerFactory.Object);
                  x.AddSingleton(_downstreamUrlTemplateVariableReplacer.Object);
                  x.AddSingleton(_scopedRepository.Object);
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
        public void happy_path()
        {
            this.Given(x => x.GivenTheDownStreamRouteIs(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(), new ReRouteBuilder().WithDownstreamTemplate("any old string").Build())))
                .And(x => x.TheUrlReplacerReturns("any old string"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }


        private void SetUpLogger()
        {
            _mockLoggerFactory = new Mock<IOcelotLoggerFactory>();

            var logger = new Mock<IOcelotLogger>();

            _mockLoggerFactory
                .Setup(x => x.CreateLogger<DownstreamUrlCreatorMiddleware>())
                .Returns(logger.Object);
        }

        private void TheUrlReplacerReturns(string downstreamUrl)
        {
            _downstreamUrl = new OkResponse<DownstreamUrl>(new DownstreamUrl(downstreamUrl));
            _downstreamUrlTemplateVariableReplacer
                .Setup(x => x.Replace(It.IsAny<string>(), It.IsAny<List<UrlPathPlaceholderNameAndValue>>()))
                .Returns(_downstreamUrl);
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
