using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Infrastructure.RequestData;
using Ocelot.RateLimit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Logging;
using System.IO;
using Ocelot.RateLimit.Middleware;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Responses;
using Xunit;
using TestStack.BDDfy;
using Ocelot.Configuration.Builder;
using Shouldly;
using Ocelot.Configuration;

namespace Ocelot.UnitTests.RateLimit
{
    public class ClientRateLimitMiddlewareTests
    {
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private OkResponse<DownstreamRoute> _downstreamRoute;
        private int responseStatusCode;

        public ClientRateLimitMiddlewareTests()
        {
            _url = "http://localhost:51879/api/ClientRateLimit";
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
             var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
                  x.AddLogging();
                  x.AddMemoryCache();
                  x.AddSingleton<IRateLimitCounterHandler, MemoryCacheRateLimitCounterHandler>();
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseRateLimiting();
                  app.Run(async context =>
                  {
                      context.Response.StatusCode = 200;
                      await context.Response.WriteAsync("This is ratelimit test");
                  });
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }


        [Fact]
        public void should_call_middleware_and_ratelimiting()
        {
            var downstreamRoute = new DownstreamRoute(new List<Ocelot.DownstreamRouteFinder.UrlMatcher.UrlPathPlaceholderNameAndValue>(),
                 new ReRouteBuilder().WithEnableRateLimiting(true).WithRateLimitOptions(
                     new Ocelot.Configuration.RateLimitOptions(true, "ClientId", new List<string>(), false, "", "", new Ocelot.Configuration.RateLimitRule("1s", TimeSpan.FromSeconds(100), 3), 429))
                     .WithUpstreamHttpMethod("Get")
                     .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .When(x => x.WhenICallTheMiddlewareMultipleTime(2))
                .Then(x => x.ThenresponseStatusCodeIs200())
                .When(x => x.WhenICallTheMiddlewareMultipleTime(2))
                .Then(x => x.ThenresponseStatusCodeIs429())
                .BDDfy();
        }

        [Fact]
        public void should_call_middleware_withWhitelistClient()
        {
            var downstreamRoute = new DownstreamRoute(new List<Ocelot.DownstreamRouteFinder.UrlMatcher.UrlPathPlaceholderNameAndValue>(),
                 new ReRouteBuilder().WithEnableRateLimiting(true).WithRateLimitOptions(
                     new Ocelot.Configuration.RateLimitOptions(true, "ClientId", new List<string>() { "ocelotclient2" }, false, "", "", new  RateLimitRule( "1s", TimeSpan.FromSeconds(100),3),429))
                     .WithUpstreamHttpMethod("Get")
                     .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .When(x => x.WhenICallTheMiddlewareWithWhiteClient())
                .Then(x => x.ThenresponseStatusCodeIs200())
                .BDDfy();
        }


        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _scopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void WhenICallTheMiddlewareMultipleTime(int times)
        {
            var clientId = "ocelotclient1";
            // Act    
            for (int i = 0; i < times; i++)
            {
                var request = new HttpRequestMessage(new HttpMethod("GET"), _url);
                request.Headers.Add("ClientId", clientId);

                var response = _client.SendAsync(request);
                responseStatusCode = (int)response.Result.StatusCode;
            }

        }

        private void WhenICallTheMiddlewareWithWhiteClient()
        {
            var clientId = "ocelotclient2";
            // Act    
            for (int i = 0; i < 10; i++)
            {
                var request = new HttpRequestMessage(new HttpMethod("GET"), _url);
                request.Headers.Add("ClientId", clientId);

                var response = _client.SendAsync(request);
                responseStatusCode = (int)response.Result.StatusCode;
            }
         }      

        private void ThenresponseStatusCodeIs429()
        {
            responseStatusCode.ShouldBe(429);
        }

        private void ThenresponseStatusCodeIs200()
        {
            responseStatusCode.ShouldBe(200);
        }
    }
}
