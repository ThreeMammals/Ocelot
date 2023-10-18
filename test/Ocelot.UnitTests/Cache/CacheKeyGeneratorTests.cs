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

        public CacheKeyGeneratorTests()
        {
            _cacheKeyGenerator = new CacheKeyGenerator();
            _downstreamRequest = new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123"));
            _downstreamRequest.Headers.Add("auth", "123456");
        }

        [Fact]
        public void should_generate_cache_key_from_context()
        {
            CacheOptions options = null;
            var cachekey = MD5Helper.GenerateMd5("GET-https://some.url/blah?abcd=123");

            this.Given(x => x.GivenDownstreamRoute(options))
                .When(x => x.WhenGenerateRequestCacheKey())
                .Then(x => x.ThenGeneratedCacheKeyIs(cachekey))
                .BDDfy();
        }

        [Fact]
        public void should_generate_cache_key_with_header_from_context()
        {
            CacheOptions options = new CacheOptions(100, "region", "auth");
            var cachekey = MD5Helper.GenerateMd5("GET-https://some.url/blah?abcd=123123456");

            this.Given(x => x.GivenDownstreamRoute(options))
                .When(x => x.WhenGenerateRequestCacheKey())
                .Then(x => x.ThenGeneratedCacheKeyIs(cachekey))
                .BDDfy();
        }

        private DownstreamRoute _downstreamRoute;

        private void GivenDownstreamRoute(CacheOptions options)
        {
            _downstreamRoute = new DownstreamRouteBuilder()
                .WithKey("key1")
                .WithCacheOptions(options)
                .Build();
        }

        private string _generatedCacheKey;

        private async Task WhenGenerateRequestCacheKey()
        {
            _generatedCacheKey = await _cacheKeyGenerator.GenerateRequestCacheKey(_downstreamRequest, _downstreamRoute);
        }

        private void ThenGeneratedCacheKeyIs(string expected)
        {
            _generatedCacheKey.ShouldBe(expected);
        }
    }
}
