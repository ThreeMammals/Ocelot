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

    public class ClientRateLimitMiddlewareTests : ServerHostedMiddlewareTest
    {
        private OkResponse<DownstreamRoute> _downstreamRoute;
        private int responseStatusCode;

        public ClientRateLimitMiddlewareTests()
        {
            GivenTheTestServerIsConfigured();
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

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddMemoryCache();
            services.AddSingleton<IRateLimitCounterHandler, MemoryCacheRateLimitCounterHandler>();
            services.AddSingleton(ScopedRepository.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseRateLimiting();
            app.Run(async context =>
            {
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("This is ratelimit test");
            });
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            ScopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void WhenICallTheMiddlewareMultipleTime(int times)
        {
            var clientId = "ocelotclient1";
  
            for (int i = 0; i < times; i++)
            {
                var request = new HttpRequestMessage(new HttpMethod("GET"), Url);
                request.Headers.Add("ClientId", clientId);

                var response = Client.SendAsync(request);
                responseStatusCode = (int)response.Result.StatusCode;
            }
        }

        private void WhenICallTheMiddlewareWithWhiteClient()
        {
            var clientId = "ocelotclient2";
 
            for (int i = 0; i < 10; i++)
            {
                var request = new HttpRequestMessage(new HttpMethod("GET"), Url);
                request.Headers.Add("ClientId", clientId);

                var response = Client.SendAsync(request);
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
