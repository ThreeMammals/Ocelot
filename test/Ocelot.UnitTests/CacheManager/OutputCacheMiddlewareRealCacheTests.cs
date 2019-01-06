﻿namespace Ocelot.UnitTests.CacheManager
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using global::CacheManager.Core;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.Cache;
    using Ocelot.Cache.CacheManager;
    using Ocelot.Cache.Middleware;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class OutputCacheMiddlewareRealCacheTests
    {
        private readonly IOcelotCache<CachedResponse> _cacheManager;
        private readonly OutputCacheMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;

        public OutputCacheMiddlewareRealCacheTests()
        {
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<OutputCacheMiddleware>()).Returns(_logger.Object);
            var cacheManagerOutputCache = CacheFactory.Build<CachedResponse>("OcelotOutputCache", x =>
            {
                x.WithDictionaryHandle();
            });
            _cacheManager = new OcelotCacheManagerCache<CachedResponse>(cacheManagerOutputCache);
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _downstreamContext.DownstreamRequest = new Ocelot.Request.Middleware.DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123"));
            _next = context => Task.CompletedTask;
            _middleware = new OutputCacheMiddleware(_next, _loggerFactory.Object, _cacheManager);
        }

        [Fact]
        public void should_cache_content_headers()
        {
            var content = new StringContent("{\"Test\": 1}")
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json")}
            };

            var response = new DownstreamResponse(content, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "fooreason");

            this.Given(x => x.GivenResponseIsNotCached(response))
                .And(x => x.GivenTheDownstreamRouteIs())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheContentTypeHeaderIsCached())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void ThenTheContentTypeHeaderIsCached()
        {
            var result = _cacheManager.Get("GET-https://some.url/blah?abcd=123", "kanken");
            var header = result.ContentHeaders["Content-Type"];
            header.First().ShouldBe("application/json");
        }

        private void GivenResponseIsNotCached(DownstreamResponse response)
        {
            _downstreamContext.DownstreamResponse = response;
        }

        private void GivenTheDownstreamRouteIs()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithIsCached(true)
                .WithCacheOptions(new CacheOptions(100, "kanken"))
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            _downstreamContext.DownstreamReRoute = reRoute;
        }
    }
}
