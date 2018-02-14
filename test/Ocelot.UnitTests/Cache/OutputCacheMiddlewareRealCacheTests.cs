using Ocelot.Infrastructure.RequestData;

namespace Ocelot.UnitTests.Cache
{
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using CacheManager.Core;
    using Shouldly;
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Cache;
    using Ocelot.Cache.Middleware;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;

    public class OutputCacheMiddlewareRealCacheTests : ServerHostedMiddlewareTest
    {
        private IOcelotCache<CachedResponse> _cacheManager;
        private CachedResponse _response;
        private IRequestScopedDataRepository _repo;

        public OutputCacheMiddlewareRealCacheTests()
        {
            ScopedRepository
                .Setup(sr => sr.Get<HttpRequestMessage>("DownstreamRequest"))
                .Returns(new OkResponse<HttpRequestMessage>(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123")));

            GivenTheTestServerIsConfigured();
        }

        [Fact]
        public void should_cache_content_headers()
        {
            var content = new StringContent("{\"Test\": 1}")
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json")}
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content,
            };

            this.Given(x => x.GivenResponseIsNotCached(response))
                .And(x => x.GivenTheDownstreamRouteIs())
                .And(x => x.GivenThereAreNoErrors())
                .And(x => x.GivenThereIsADownstreamUrl())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheContentTypeHeaderIsCached())
                .BDDfy();
        }

        private void ThenTheContentTypeHeaderIsCached()
        {
            var result = _cacheManager.Get("GET-https://some.url/blah?abcd=123", "kanken");
            var header = result.ContentHeaders["Content-Type"];
            header.First().ShouldBe("application/json");
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            var cacheManagerOutputCache = CacheFactory.Build<CachedResponse>("OcelotOutputCache", x =>
            {
                x.WithDictionaryHandle();
            });

            _cacheManager = new OcelotCacheManagerCache<CachedResponse>(cacheManagerOutputCache);

            services.AddSingleton<ICacheManager<CachedResponse>>(cacheManagerOutputCache);
            services.AddSingleton<IOcelotCache<CachedResponse>>(_cacheManager);

            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();

            services.AddLogging();
            services.AddSingleton(_cacheManager);
            services.AddSingleton(ScopedRepository.Object);
            services.AddSingleton<IRegionCreator, RegionCreator>();
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseOutputCacheMiddleware();
        }

        private void GivenResponseIsNotCached(HttpResponseMessage message)
        {
            ScopedRepository
                .Setup(x => x.Get<HttpResponseMessage>("HttpResponseMessage"))
                .Returns(new OkResponse<HttpResponseMessage>(message));
        }

        private void GivenTheDownstreamRouteIs()
        {
            var reRoute = new ReRouteBuilder()
                .WithIsCached(true)
                .WithCacheOptions(new CacheOptions(100, "kanken"))
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            var downstreamRoute = new DownstreamRoute(new List<PlaceholderNameAndValue>(), reRoute);

            ScopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(new OkResponse<DownstreamRoute>(downstreamRoute));
        }

        private void GivenThereAreNoErrors()
        {
            ScopedRepository
                .Setup(x => x.Get<bool>("OcelotMiddlewareError"))
                .Returns(new OkResponse<bool>(false));
        }

        private void GivenThereIsADownstreamUrl()
        {
            ScopedRepository
                .Setup(x => x.Get<string>("DownstreamUrl"))
                .Returns(new OkResponse<string>("anything"));
        }
    }
}
