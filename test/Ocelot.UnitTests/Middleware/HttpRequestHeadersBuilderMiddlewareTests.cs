using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Library.Configuration;
using Ocelot.Library.Configuration.Builder;
using Ocelot.Library.DownstreamRouteFinder;
using Ocelot.Library.Middleware;
using Ocelot.Library.Repository;
using Ocelot.Library.RequestBuilder;
using Ocelot.Library.Responses;
using Ocelot.Library.UrlMatcher;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Middleware
{
    public class HttpRequestHeadersBuilderMiddlewareTests : IDisposable
    {
        private readonly Mock<IScopedRequestDataRepository> _scopedRepository;
        private readonly Mock<IAddHeadersToRequest> _addHeaders;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private Response<DownstreamRoute> _downstreamRoute;
        private HttpResponseMessage _result;

        public HttpRequestHeadersBuilderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _scopedRepository = new Mock<IScopedRequestDataRepository>();
            _addHeaders = new Mock<IAddHeadersToRequest>();
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton(_addHeaders.Object);
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseHttpRequestHeadersBuilderMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void happy_path()
        {
            var downstreamRoute = new DownstreamRoute(new List<TemplateVariableNameAndValue>(),
                new ReRouteBuilder()
                    .WithDownstreamTemplate("any old string")
                    .WithConfigurationHeaderExtractorProperties(new List<ClaimToHeader>
                    {
                        new ClaimToHeader("UserId", "Subject", "", 0)
                    })
                    .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheAddHeadersToRequestReturns("123"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheAddHeadersToRequestIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheAddHeadersToRequestReturns(string claimValue)
        {
            _addHeaders
                .Setup(x => x.SetHeadersOnContext(It.IsAny<List<ClaimToHeader>>(), 
                It.IsAny<HttpContext>()))
                .Returns(new OkResponse());
        }

        private void ThenTheAddHeadersToRequestIsCalledCorrectly()
        {
            _addHeaders
                .Verify(x => x.SetHeadersOnContext(It.IsAny<List<ClaimToHeader>>(),
                It.IsAny<HttpContext>()), Times.Once);
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
