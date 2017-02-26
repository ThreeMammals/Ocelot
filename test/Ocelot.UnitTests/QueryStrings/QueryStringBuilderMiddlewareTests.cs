using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Headers.Middleware;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.QueryStrings;
using Ocelot.QueryStrings.Middleware;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.QueryStrings
{
    public class QueryStringBuilderMiddlewareTests : IDisposable
    {
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly Mock<IAddQueriesToRequest> _addQueries;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private Response<DownstreamRoute> _downstreamRoute;
        private HttpResponseMessage _result;

        public QueryStringBuilderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
            _addQueries = new Mock<IAddQueriesToRequest>();
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
                  x.AddLogging();
                  x.AddSingleton(_addQueries.Object);
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseQueryStringBuilderMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void should_call_add_queries_correctly()
        {
            var downstreamRoute = new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("any old string")
                    .WithClaimsToQueries(new List<ClaimToThing>
                    {
                        new ClaimToThing("UserId", "Subject", "", 0)
                    })
                    .WithUpstreamHttpMethod("Get")
                    .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheAddHeadersToRequestReturns())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheAddQueriesToRequestIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheAddHeadersToRequestReturns()
        {
            _addQueries
                .Setup(x => x.SetQueriesOnContext(It.IsAny<List<ClaimToThing>>(), 
                It.IsAny<HttpContext>()))
                .Returns(new OkResponse());
        }

        private void ThenTheAddQueriesToRequestIsCalledCorrectly()
        {
            _addQueries
                .Verify(x => x.SetQueriesOnContext(It.IsAny<List<ClaimToThing>>(),
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
