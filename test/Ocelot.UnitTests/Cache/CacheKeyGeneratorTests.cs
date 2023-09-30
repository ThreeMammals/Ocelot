using Ocelot.Cache;
using Ocelot.Request.Middleware;

namespace Ocelot.UnitTests.Cache
{
    public class CacheKeyGeneratorTests
    {
        private readonly ICacheKeyGenerator _cacheKeyGenerator;
        private readonly DownstreamRequest _downstreamRequest;

        public CacheKeyGeneratorTests()
        {
            _cacheKeyGenerator = new CacheKeyGenerator();
            _cacheKeyGenerator = new CacheKeyGenerator();
            _downstreamRequest = new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123"));
        }

        [Fact]
        public void should_generate_cache_key_from_context()
        {
            this.Given(x => x.GivenCacheKeyFromContext(_downstreamRequest))
                .BDDfy();
        }

        private void GivenCacheKeyFromContext(DownstreamRequest downstreamRequest)
        {
            var generatedCacheKey = _cacheKeyGenerator.GenerateRequestCacheKey(downstreamRequest);
            var cachekey = MD5Helper.GenerateMd5("GET-https://some.url/blah?abcd=123");
            generatedCacheKey.ShouldBe(cachekey);
        }
    }
}
