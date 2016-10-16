using Ocelot.Library.Infrastructure.Builder;
using Ocelot.Library.Infrastructure.Middleware;

namespace Ocelot.UnitTests.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using Library.Infrastructure.Configuration;
    using Library.Infrastructure.DownstreamRouteFinder;
    using Library.Infrastructure.Repository;
    using Library.Infrastructure.Responses;
    using Library.Infrastructure.UrlMatcher;
    using Library.Infrastructure.UrlTemplateReplacer;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using TestStack.BDDfy;
    using Xunit;

    public class DownstreamUrlCreatorMiddlewareTests : IDisposable
    {
        private readonly Mock<IDownstreamUrlTemplateVariableReplacer> _downstreamUrlTemplateVariableReplacer;
        private readonly Mock<IScopedRequestDataRepository> _scopedRepository;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private Response<DownstreamRoute> _downstreamRoute;
        private HttpResponseMessage _result;
        private OkResponse<string> _downstreamUrl;

        public DownstreamUrlCreatorMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _downstreamUrlTemplateVariableReplacer = new Mock<IDownstreamUrlTemplateVariableReplacer>();
            _scopedRepository = new Mock<IScopedRequestDataRepository>();

            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
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
            this.Given(x => x.GivenTheDownStreamRouteIs(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), new ReRouteBuilder().WithDownstreamTemplate("any old string").Build())))
                .And(x => x.TheUrlReplacerReturns("any old string"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        private void TheUrlReplacerReturns(string downstreamUrl)
        {
            _downstreamUrl = new OkResponse<string>(downstreamUrl);
            _downstreamUrlTemplateVariableReplacer
                .Setup(x => x.ReplaceTemplateVariables(It.IsAny<DownstreamRoute>()))
                .Returns(_downstreamUrl);
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            _scopedRepository
                .Verify(x => x.Add("DownstreamUrl", _downstreamUrl.Data), Times.Once());
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
