namespace Ocelot.UnitTests.RateLimit
{
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.Logging;
    using Ocelot.RateLimit;
    using Ocelot.RateLimit.Middleware;
    using Ocelot.Responses;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Microsoft.Extensions.Caching.Memory;
    using System.Text;
    using System.IO;
    using Microsoft.AspNetCore.Http.Internal;

    public class ClientRateLimitMiddlewareTests
    {
        private OkResponse<DownstreamRoute> _downstreamRoute;
        private int _responseStatusCode;
        private IRateLimitCounterHandler _rateLimitCounterHandler;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private ClientRateLimitMiddleware _middleware;
        private DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;
        private string _url;

        public ClientRateLimitMiddlewareTests()
        {
            _url = "http://localhost:51879";
            var cacheEntryOptions = new MemoryCacheOptions();
            _rateLimitCounterHandler = new MemoryCacheRateLimitCounterHandler(new MemoryCache(cacheEntryOptions));
            var httpContext = new DefaultHttpContext();
            var httpResponse = new DefaultHttpResponse(httpContext);
            var httpRequest = new DefaultHttpRequest(httpContext);

            _downstreamContext = new DownstreamContext(httpContext);
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<ClientRateLimitMiddleware>()).Returns(_logger.Object);
             _next = async (context) => {
                context.HttpContext.Response.StatusCode = 200;
                byte[] byteArray = Encoding.ASCII.GetBytes("This is ratelimit test");
                MemoryStream stream = new MemoryStream(byteArray);
                context.HttpContext.Response.Body = stream;
            };
            _middleware = new ClientRateLimitMiddleware(_next, _loggerFactory.Object, _rateLimitCounterHandler);
        }

        [Fact]
        public void should_call_middleware_and_ratelimiting()
        {
            var downstreamRoute = new DownstreamRoute(new List<Ocelot.DownstreamRouteFinder.UrlMatcher.PlaceholderNameAndValue>(),
                 new ReRouteBuilder().WithEnableRateLimiting(true).WithRateLimitOptions(
                     new Ocelot.Configuration.RateLimitOptions(true, "ClientId", new List<string>(), false, "", "", new Ocelot.Configuration.RateLimitRule("1s", 100, 3), 429))
                     .WithUpstreamHttpMethod(new List<string> { "Get" })
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
            var downstreamRoute = new DownstreamRoute(new List<Ocelot.DownstreamRouteFinder.UrlMatcher.PlaceholderNameAndValue>(),
                 new ReRouteBuilder().WithEnableRateLimiting(true).WithRateLimitOptions(
                     new Ocelot.Configuration.RateLimitOptions(true, "ClientId", new List<string>() { "ocelotclient2" }, false, "", "", new  RateLimitRule( "1s", 100,3),429))
                     .WithUpstreamHttpMethod(new List<string> { "Get" })
                     .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .When(x => x.WhenICallTheMiddlewareWithWhiteClient())
                .Then(x => x.ThenresponseStatusCodeIs200())
                .BDDfy();
        }

        // protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        // {
        //     services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
        //     services.AddLogging();
        //     services.AddMemoryCache();
        //     services.AddSingleton<IRateLimitCounterHandler, MemoryCacheRateLimitCounterHandler>();
        //     services.AddSingleton(ScopedRepository.Object);
        // }

        // protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        // {
        //     app.UseRateLimiting();
        //     app.Run(async context =>
        //     {
        //         context.Response.StatusCode = 200;
        //         await context.Response.WriteAsync("This is ratelimit test");
        //     });
        // }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _downstreamContext.DownstreamRoute = downstreamRoute;
            _downstreamContext.DownstreamReRoute = downstreamRoute.ReRoute.DownstreamReRoute[0];
        }

        private void WhenICallTheMiddlewareMultipleTime(int times)
        {
            var clientId = "ocelotclient1";
  
            for (int i = 0; i < times; i++)
            {
                var request = new HttpRequestMessage(new HttpMethod("GET"), _url);
                request.Headers.Add("ClientId", clientId);
                _downstreamContext.DownstreamRequest = request;

                //var response = Client.SendAsync(request);
                _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
                _responseStatusCode = (int)_downstreamContext.HttpContext.Response.StatusCode;
            }
        }

        private void WhenICallTheMiddlewareWithWhiteClient()
        {
            var clientId = "ocelotclient2";
 
            for (int i = 0; i < 10; i++)
            {
                var request = new HttpRequestMessage(new HttpMethod("GET"), _url);
                request.Headers.Add("ClientId", clientId);
                _downstreamContext.DownstreamRequest = request;


                //var response = Client.SendAsync(request);
                _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
                _responseStatusCode = (int)_downstreamContext.HttpContext.Response.StatusCode;            
            }
         }      

        private void ThenresponseStatusCodeIs429()
        {
            _responseStatusCode.ShouldBe(429);
        }

        private void ThenresponseStatusCodeIs200()
        {
            _responseStatusCode.ShouldBe(200);
        }
    }
}
