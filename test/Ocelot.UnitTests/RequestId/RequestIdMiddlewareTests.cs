using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.RequestId.Middleware;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.RequestId
{
    public class RequestIdMiddlewareTests
    {
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly HttpRequestMessage _downstreamRequest;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private Response<DownstreamRoute> _downstreamRoute;
        private HttpResponseMessage _result;
        private string _value;
        private string _key;

        public RequestIdMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
                  x.AddLogging();
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseRequestIdMiddleware();

                  app.Run(x =>
                  {
                      x.Response.Headers.Add("LSRequestId", x.TraceIdentifier);
                      return Task.CompletedTask;
                  });
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();

            _downstreamRequest = new HttpRequestMessage();

            _scopedRepository
                .Setup(sr => sr.Get<HttpRequestMessage>("DownstreamRequest"))
                .Returns(new OkResponse<HttpRequestMessage>(_downstreamRequest));
        }

        [Fact]
        public void should_pass_down_request_id_from_upstream_request()
        {
            var downstreamRoute = new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(),
                new ReRouteBuilder()
                .WithDownstreamPathTemplate("any old string")
                .WithRequestIdKey("LSRequestId")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build());

            var requestId = Guid.NewGuid().ToString();

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheRequestIdIsAddedToTheRequest("LSRequestId", requestId))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheTraceIdIs(requestId))
                .BDDfy();
        }

        [Fact]
        public void should_add_request_id_when_not_on_upstream_request()
        {
            var downstreamRoute = new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(),
                new ReRouteBuilder()
                .WithDownstreamPathTemplate("any old string")
                .WithRequestIdKey("LSRequestId")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheTraceIdIsAnything())
                .BDDfy();
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _scopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void GivenTheRequestIdIsAddedToTheRequest(string key, string value)
        {
            _key = key;
            _value = value;
            _client.DefaultRequestHeaders.TryAddWithoutValidation(_key, _value);
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void ThenTheTraceIdIsAnything()
        {
            _result.Headers.GetValues("LSRequestId").First().ShouldNotBeNullOrEmpty();
        }

        private void ThenTheTraceIdIs(string expected)
        {
            _result.Headers.GetValues("LSRequestId").First().ShouldBe(expected);
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}
