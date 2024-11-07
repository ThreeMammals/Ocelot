using CacheManager.Core;
using Microsoft.AspNetCore.Http;
using Ocelot.Cache;
using Ocelot.Cache.CacheManager;
using Ocelot.Cache.Middleware;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Net.Http.Headers;

namespace Ocelot.UnitTests.CacheManager
{
    public class OutputCacheMiddlewareRealCacheTests : UnitTest
    {
        private readonly IOcelotCache<CachedResponse> _cacheManager;
        private readonly ICacheKeyGenerator _cacheKeyGenerator;
        private readonly OutputCacheMiddleware _middleware;
        private readonly RequestDelegate _next;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private readonly Mock<IOcelotLogger> _logger;
        private readonly HttpContext _httpContext;

        public OutputCacheMiddlewareRealCacheTests()
        {
            _httpContext = new DefaultHttpContext();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<OutputCacheMiddleware>()).Returns(_logger.Object);
            var cacheManagerOutputCache = CacheFactory.Build<CachedResponse>("OcelotOutputCache", x =>
            {
                x.WithDictionaryHandle();
            });
            _cacheManager = new OcelotCacheManagerCache<CachedResponse>(cacheManagerOutputCache);
            _cacheKeyGenerator = new DefaultCacheKeyGenerator();
            _httpContext.Items.UpsertDownstreamRequest(new Ocelot.Request.Middleware.DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123")));
            _next = context => Task.CompletedTask;
            _middleware = new OutputCacheMiddleware(_next, _loggerFactory.Object, _cacheManager, _cacheKeyGenerator);
        }

        [Fact]
        public void should_cache_content_headers()
        {
            var content = new StringContent("{\"Test\": 1}")
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") },
            };

            var response = new DownstreamResponse(content, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "fooreason");

            this.Given(x => x.GivenResponseIsNotCached(response))
                .And(x => x.GivenTheDownstreamRouteIs())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheContentTypeHeaderIsCached())
                .BDDfy();
        }

        private async Task WhenICallTheMiddleware()
        {
            await _middleware.Invoke(_httpContext);
        }

        private void ThenTheContentTypeHeaderIsCached()
        {
            var cacheKey = MD5Helper.GenerateMd5("GET-https://some.url/blah?abcd=123");
            var result = _cacheManager.Get(cacheKey, "kanken");
            var header = result.ContentHeaders["Content-Type"];
            header.First().ShouldBe("application/json");
        }

        private void GivenResponseIsNotCached(DownstreamResponse response)
        {
            _httpContext.Items.UpsertDownstreamResponse(response);
        }

        private void GivenTheDownstreamRouteIs()
        {
            var route = new DownstreamRouteBuilder()
                .WithIsCached(true)
                .WithCacheOptions(new CacheOptions(100, "kanken", null, false))
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            _httpContext.Items.UpsertDownstreamRoute(route);
        }
    }
}
