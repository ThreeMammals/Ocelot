using Microsoft.AspNetCore.Http;
using Ocelot.Cache;
using Ocelot.Cache.Middleware;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.UnitTests.Cache
{
    public class OutputCacheMiddlewareTests : UnitTest
    {
        private readonly Mock<IOcelotCache<CachedResponse>> _cache;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private readonly Mock<IOcelotLogger> _logger;
        private OutputCacheMiddleware _middleware;
        private readonly RequestDelegate _next;
        private readonly ICacheKeyGenerator _cacheKeyGenerator;
        private CachedResponse _response;
        private readonly HttpContext _httpContext;
        private Mock<IRequestScopedDataRepository> _repo;

        public OutputCacheMiddlewareTests()
        {
            _repo = new Mock<IRequestScopedDataRepository>();
            _httpContext = new DefaultHttpContext();
            _cache = new Mock<IOcelotCache<CachedResponse>>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _cacheKeyGenerator = new DefaultCacheKeyGenerator();
            _loggerFactory.Setup(x => x.CreateLogger<OutputCacheMiddleware>()).Returns(_logger.Object);
            _next = context => Task.CompletedTask;
            _httpContext.Items.UpsertDownstreamRequest(new Ocelot.Request.Middleware.DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123")));
        }

        [Fact]
        public void should_returned_cached_item_when_it_is_in_cache()
        {
            var headers = new Dictionary<string, IEnumerable<string>>
            {
                { "test", new List<string> { "test" } },
            };

            var contentHeaders = new Dictionary<string, IEnumerable<string>>
            {
                { "content-type", new List<string> { "application/json" } },
            };

            var cachedResponse = new CachedResponse(HttpStatusCode.OK, headers, string.Empty, contentHeaders, "some reason");
            this.Given(x => x.GivenThereIsACachedResponse(cachedResponse))
                .And(x => x.GivenTheDownstreamRouteIs())
                .When(x => x.WhenICallTheMiddlewareAsync())
                .Then(x => x.ThenTheCacheGetIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_returned_cached_item_when_it_is_in_cache_expires_header()
        {
            var contentHeaders = new Dictionary<string, IEnumerable<string>>
            {
                { "Expires", new List<string> { "-1" } },
            };

            var cachedResponse = new CachedResponse(HttpStatusCode.OK, new Dictionary<string, IEnumerable<string>>(), string.Empty, contentHeaders, "some reason");
            this.Given(x => x.GivenThereIsACachedResponse(cachedResponse))
                .And(x => x.GivenTheDownstreamRouteIs())
                .When(x => x.WhenICallTheMiddlewareAsync())
                .Then(x => x.ThenTheCacheGetIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_continue_with_pipeline_and_cache_response()
        {
            this.Given(x => x.GivenResponseIsNotCached(new HttpResponseMessage()))
                .And(x => x.GivenTheDownstreamRouteIs())
                .When(x => x.WhenICallTheMiddlewareAsync())
                .Then(x => x.ThenTheCacheAddIsCalledCorrectly())
                .BDDfy();
        }

        private async Task WhenICallTheMiddlewareAsync()
        {
            _middleware = new OutputCacheMiddleware(_next, _loggerFactory.Object, _cache.Object, _cacheKeyGenerator);
            await _middleware.Invoke(_httpContext);
        }

        private void GivenThereIsACachedResponse(CachedResponse response)
        {
            _response = response;
            _cache
              .Setup(x => x.Get(It.IsAny<string>(), It.IsAny<string>()))
              .Returns(_response);
        }

        private void GivenResponseIsNotCached(HttpResponseMessage responseMessage)
        {
            _httpContext.Items.UpsertDownstreamResponse(new DownstreamResponse(responseMessage));
        }

        private void GivenTheDownstreamRouteIs()
        {
            var route = new RouteBuilder()
                .WithDownstreamRoute(new DownstreamRouteBuilder()
                    .WithIsCached(true)
                    .WithCacheOptions(new CacheOptions(100, "kanken", null, false))
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            var downstreamRoute = new Ocelot.DownstreamRouteFinder.DownstreamRouteHolder(new List<PlaceholderNameAndValue>(), route);

            _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);

            _httpContext.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
        }

        private void ThenTheCacheGetIsCalledCorrectly()
        {
            _cache
                .Verify(x => x.Get(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private void ThenTheCacheAddIsCalledCorrectly()
        {
            _cache
                .Verify(x => x.Add(It.IsAny<string>(), It.IsAny<CachedResponse>(), It.IsAny<TimeSpan>(), It.IsAny<string>()), Times.Once);
        }
    }
}
