namespace Ocelot.UnitTests.Cache
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.Cache;
    using Ocelot.Cache.Middleware;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using TestStack.BDDfy;
    using Xunit;
    using System.Net;
    using Ocelot.Middleware;
    using Ocelot.Middleware.Multiplexer;

    public class OutputCacheMiddlewareTests
    {
        private readonly Mock<IOcelotCache<CachedResponse>> _cacheManager;    
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private OutputCacheMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private readonly OcelotRequestDelegate _next;
        private CachedResponse _response;
        private readonly IRegionCreator _regionCreator;

        public OutputCacheMiddlewareTests()
        {
            _cacheManager = new Mock<IOcelotCache<CachedResponse>>();
            _regionCreator = new RegionCreator();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<OutputCacheMiddleware>()).Returns(_logger.Object);
            _next = context => Task.CompletedTask;
            _downstreamContext.DownstreamRequest = new Ocelot.Request.Middleware.DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123"));
        }

        [Fact]
        public void should_returned_cached_item_when_it_is_in_cache()
        {
            var headers = new Dictionary<string, IEnumerable<string>>
            {
                { "test", new List<string> { "test" } }
            };

            var contentHeaders = new Dictionary<string, IEnumerable<string>>
            {
                { "content-type", new List<string> { "application/json" } }
            };

            var cachedResponse = new CachedResponse(HttpStatusCode.OK, headers, "", contentHeaders);
            this.Given(x => x.GivenThereIsACachedResponse(cachedResponse))
                .And(x => x.GivenTheDownstreamRouteIs())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheCacheGetIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_continue_with_pipeline_and_cache_response()
        {
            this.Given(x => x.GivenResponseIsNotCached())
                .And(x => x.GivenTheDownstreamRouteIs())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheCacheAddIsCalledCorrectly())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware = new OutputCacheMiddleware(_next, _loggerFactory.Object, _cacheManager.Object, _regionCreator);
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenThereIsACachedResponse(CachedResponse response)
        {
            _response = response;
            _cacheManager
              .Setup(x => x.Get(It.IsAny<string>(), It.IsAny<string>()))
              .Returns(_response);
        }

        private void GivenResponseIsNotCached()
        {
            _downstreamContext.DownstreamResponse = new DownstreamResponse(new HttpResponseMessage());
        }

        private void GivenTheDownstreamRouteIs()
        {
            var reRoute = new ReRouteBuilder()
                .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                    .WithIsCached(true)
                    .WithCacheOptions(new CacheOptions(100, "kanken"))
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();
                
            var downstreamRoute = new DownstreamRoute(new List<PlaceholderNameAndValue>(), reRoute);

            _downstreamContext.TemplatePlaceholderNameAndValues = downstreamRoute.TemplatePlaceholderNameAndValues;
            _downstreamContext.DownstreamReRoute = downstreamRoute.ReRoute.DownstreamReRoute[0];
        }

        private void ThenTheCacheGetIsCalledCorrectly()
        {
            _cacheManager
                .Verify(x => x.Get(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private void ThenTheCacheAddIsCalledCorrectly()
        {
            _cacheManager
                .Verify(x => x.Add(It.IsAny<string>(), It.IsAny<CachedResponse>(), It.IsAny<TimeSpan>(), It.IsAny<string>()), Times.Once);
        }
    }
}
