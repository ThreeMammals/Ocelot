namespace Ocelot.UnitTests.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using Library.Infrastructure.DownstreamRouteFinder;
    using Library.Infrastructure.Repository;
    using Library.Infrastructure.Responses;
    using Library.Infrastructure.UrlMatcher;
    using Library.Middleware;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using TestStack.BDDfy;
    using Xunit;

    public class DownstreamRouteFinderMiddlewareTests : IDisposable
    {
        private readonly Mock<IDownstreamRouteFinder> _downstreamRouteFinder;
        private readonly Mock<IScopedRequestDataRepository> _scopedRepository;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private Response<DownstreamRoute> _downstreamRoute;
        private HttpResponseMessage _result;

        public DownstreamRouteFinderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _downstreamRouteFinder = new Mock<IDownstreamRouteFinder>();
            _scopedRepository = new Mock<IScopedRequestDataRepository>();

            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton(_downstreamRouteFinder.Object);
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseDownstreamRouteFinderMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void happy_path()
        {
            this.Given(x => x.GivenTheDownStreamRouteFinderReturns(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), "any old string")))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            _scopedRepository
                .Verify(x => x.Add("DownstreamRoute", _downstreamRoute.Data), Times.Once());
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void GivenTheDownStreamRouteFinderReturns(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _downstreamRouteFinder
                .Setup(x => x.FindDownstreamRoute(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}
