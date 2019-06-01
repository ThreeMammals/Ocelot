using Ocelot.Middleware;

namespace Ocelot.UnitTests.RateLimit
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Memory;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.Logging;
    using Ocelot.RateLimit;
    using Ocelot.RateLimit.Middleware;
    using Ocelot.Request.Middleware;
    using Shouldly;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TestStack.BDDfy;
    using Xunit;

    public class ClientRateLimitMiddlewareTests
    {
        private int _responseStatusCode;
        private IRateLimitCounterHandler _rateLimitCounterHandler;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly ClientRateLimitMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;
        private readonly string _url;

        public ClientRateLimitMiddlewareTests()
        {
            _url = "http://localhost:51879";
            var cacheEntryOptions = new MemoryCacheOptions();
            _rateLimitCounterHandler = new MemoryCacheRateLimitCounterHandler(new MemoryCache(cacheEntryOptions));
            var httpContext = new DefaultHttpContext();
            _downstreamContext = new DownstreamContext(httpContext);
            _downstreamContext.HttpContext.Response.Body = new FakeStream();

            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<ClientRateLimitMiddleware>()).Returns(_logger.Object);
            _next = context => Task.CompletedTask;
            _middleware = new ClientRateLimitMiddleware(_next, _loggerFactory.Object, _rateLimitCounterHandler);
        }

        [Fact]
        public void should_call_middleware_and_ratelimiting()
        {
            var upstreamTemplate = new UpstreamPathTemplateBuilder().Build();

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithEnableRateLimiting(true)
                .WithRateLimitOptions(new RateLimitOptions(true, "ClientId", new List<string>(), false, "", "", new RateLimitRule("1s", 100, 3), 429))
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .WithUpstreamPathTemplate(upstreamTemplate)
                .Build();

            var reRoute = new ReRouteBuilder()
                .WithDownstreamReRoute(downstreamReRoute)
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            var downstreamRoute = new DownstreamRoute(new List<Ocelot.DownstreamRouteFinder.UrlMatcher.PlaceholderNameAndValue>(), reRoute);

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
                 new ReRouteBuilder()
                     .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                         .WithEnableRateLimiting(true)
                         .WithRateLimitOptions(
                             new Ocelot.Configuration.RateLimitOptions(true, "ClientId", new List<string>() { "ocelotclient2" }, false, "", "", new RateLimitRule("1s", 100, 3), 429))
                         .WithUpstreamHttpMethod(new List<string> { "Get" })
                         .Build())
                     .WithUpstreamHttpMethod(new List<string> { "Get" })
                     .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .When(x => x.WhenICallTheMiddlewareWithWhiteClient())
                .Then(x => x.ThenresponseStatusCodeIs200())
                .BDDfy();
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamContext.TemplatePlaceholderNameAndValues = downstreamRoute.TemplatePlaceholderNameAndValues;
            _downstreamContext.DownstreamReRoute = downstreamRoute.ReRoute.DownstreamReRoute[0];
        }

        private void WhenICallTheMiddlewareMultipleTime(int times)
        {
            var clientId = "ocelotclient1";

            for (int i = 0; i < times; i++)
            {
                var request = new HttpRequestMessage(new HttpMethod("GET"), _url);
                request.Headers.Add("ClientId", clientId);
                _downstreamContext.DownstreamRequest = new DownstreamRequest(request);

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
                _downstreamContext.DownstreamRequest = new DownstreamRequest(request);
                _downstreamContext.HttpContext.Request.Headers.TryAdd("ClientId", clientId);

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

    internal class FakeStream : Stream
    {
        public override void Flush()
        {
            throw new System.NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            //do nothing
        }

        public override bool CanRead { get; }
        public override bool CanSeek { get; }
        public override bool CanWrite => true;
        public override long Length { get; }
        public override long Position { get; set; }
    }
}
