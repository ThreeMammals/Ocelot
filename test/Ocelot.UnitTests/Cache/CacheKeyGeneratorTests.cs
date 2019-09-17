using Microsoft.AspNetCore.Http;
using Ocelot.Cache;
using Ocelot.Middleware;
using Shouldly;
using System.Net.Http;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Cache
{
    public class CacheKeyGeneratorTests
    {
        private readonly ICacheKeyGenerator _cacheKeyGenerator;
        private readonly DownstreamContext _downstreamContext;

        public CacheKeyGeneratorTests()
        {
            _cacheKeyGenerator = new CacheKeyGenerator();
            _cacheKeyGenerator = new CacheKeyGenerator();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamRequest = new Ocelot.Request.Middleware.DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123"))
            };
        }

        [Fact]
        public void should_generate_cache_key_from_context()
        {
            this.Given(x => x.GivenCacheKeyFromContext(_downstreamContext))
                .BDDfy();
        }

        private void GivenCacheKeyFromContext(DownstreamContext context)
        {
            string generatedCacheKey = _cacheKeyGenerator.GenerateRequestCacheKey(context);
            string cachekey = MD5Helper.GenerateMd5("GET-https://some.url/blah?abcd=123");
            generatedCacheKey.ShouldBe(cachekey);
        }
    }
}
