using Ocelot.Cache;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Request.Middleware;

namespace Ocelot.UnitTests.Cache
{
    public class CacheKeyGeneratorTests
    {
        private readonly ICacheKeyGenerator _cacheKeyGenerator;
        private readonly DownstreamRequest _downstreamRequest;
        private DownstreamRoute _downstreamRoute;

        public CacheKeyGeneratorTests()
        {
            _cacheKeyGenerator = new CacheKeyGenerator();
            _cacheKeyGenerator = new CacheKeyGenerator();
            _downstreamRequest = new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123")
            {
                Headers = { { "auth", "123456" } },
            });
            _downstreamRoute = new DownstreamRouteBuilder().WithKey("key1").Build();
        }

        [Fact]
        public void should_generate_cache_key_from_context()
        {
            this.Given(x => x.GivenCacheKeyFromContext(_downstreamRequest, _downstreamRoute))
                .BDDfy();
        }

        [Fact]
        public void should_generate_cache_key_with_header_from_context()
        {
            _downstreamRoute = new DownstreamRouteBuilder().WithCacheOptions(new CacheOptions(100, "test", "auth")).WithKey("key1").Build();

            this.Given(x => x.GivenCacheKeyWithHeaderFromContext(_downstreamRequest, _downstreamRoute))
                .BDDfy();
        }

        private async Task GivenCacheKeyFromContext(DownstreamRequest downstreamRequest, DownstreamRoute downstreamRoute)
        {
            var generatedCacheKey = await _cacheKeyGenerator.GenerateRequestCacheKey(downstreamRequest, downstreamRoute);
            var cachekey = MD5Helper.GenerateMd5("GET-https://some.url/blah?abcd=123");
            generatedCacheKey.ShouldBe(cachekey);
        }

        private async Task GivenCacheKeyWithHeaderFromContext(DownstreamRequest downstreamRequest, DownstreamRoute downstreamRoute)
        {
            var generatedCacheKey = await _cacheKeyGenerator.GenerateRequestCacheKey(downstreamRequest, downstreamRoute);
            var cachekey = MD5Helper.GenerateMd5("GET-https://some.url/blah?abcd=123123456");
            generatedCacheKey.ShouldBe(cachekey);
        }
    }
}
