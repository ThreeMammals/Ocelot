using Ocelot.Cache;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Request.Middleware;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace Ocelot.UnitTests.Cache
{
    public class CacheKeyGeneratorTests
    {
        private readonly ICacheKeyGenerator _cacheKeyGenerator;
        private readonly Mock<DownstreamRequest> _downstreamRequest;

        private const string verb = "GET";
        private const string url = "https://some.url/blah?abcd=123";
        private const string header = nameof(CacheKeyGeneratorTests);
        private const string headerName = "auth";

        public CacheKeyGeneratorTests()
        {
            _cacheKeyGenerator = new CacheKeyGenerator();

            _downstreamRequest = new Mock<DownstreamRequest>();
            _downstreamRequest.SetupGet(x => x.Method).Returns(verb);
            _downstreamRequest.SetupGet(x => x.OriginalString).Returns(url);

            var headers = new HttpHeadersStub
            {
                { headerName, header },
            };
            _downstreamRequest.SetupGet(x => x.Headers).Returns(headers);
        }

        [Fact]
        public void should_generate_cache_key_with_request_content()
        {
            const string content = nameof(should_generate_cache_key_with_request_content);
            var httpRequest = new HttpRequestMessageStub(content);
            _downstreamRequest.SetupGet(x => x.HasContent).Returns(true);
            _downstreamRequest.SetupGet(x => x.Request).Returns(httpRequest);

            var cachekey = MD5Helper.GenerateMd5($"{verb}-{url}-{content}");

            this.Given(x => x.GivenDownstreamRoute(null))
                .When(x => x.WhenGenerateRequestCacheKey())
                .Then(x => x.ThenGeneratedCacheKeyIs(cachekey))
                .BDDfy();
        }

        [Fact]
        public void should_generate_cache_key_without_request_content()
        {
            _downstreamRequest.SetupGet(x => x.HasContent).Returns(false);

            CacheOptions options = null;
            var cachekey = MD5Helper.GenerateMd5($"{verb}-{url}");

            this.Given(x => x.GivenDownstreamRoute(options))
                .When(x => x.WhenGenerateRequestCacheKey())
                .Then(x => x.ThenGeneratedCacheKeyIs(cachekey))
                .BDDfy();
        }

        [Fact]
        public void should_generate_cache_key_with_cache_options_header()
        {
            _downstreamRequest.SetupGet(x => x.HasContent).Returns(false);

            CacheOptions options = new CacheOptions(100, "region", headerName);
            var cachekey = MD5Helper.GenerateMd5($"{verb}-{url}-{header}");

            this.Given(x => x.GivenDownstreamRoute(options))
                .When(x => x.WhenGenerateRequestCacheKey())
                .Then(x => x.ThenGeneratedCacheKeyIs(cachekey))
                .BDDfy();
        }

        [Fact]
        public void should_generate_cache_key_happy_path()
        {
            const string content = nameof(should_generate_cache_key_happy_path);

            var httpRequest = new HttpRequestMessageStub(content);
            _downstreamRequest.SetupGet(x => x.HasContent).Returns(true);
            _downstreamRequest.SetupGet(x => x.Request).Returns(httpRequest);

            CacheOptions options = new CacheOptions(100, "region", headerName);
            var cachekey = MD5Helper.GenerateMd5($"{verb}-{url}-{header}-{content}");

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
            _generatedCacheKey = await _cacheKeyGenerator.GenerateRequestCacheKey(_downstreamRequest.Object, _downstreamRoute);
        }

        private void ThenGeneratedCacheKeyIs(string expected)
        {
            _generatedCacheKey.ShouldBe(expected);
        }
    }

    internal class HttpHeadersStub : HttpHeaders
    {
        public HttpHeadersStub() : base() { }
    }

    internal class HttpRequestMessageStub : HttpRequestMessage
    {
        private readonly string _content;
        private readonly HttpContent _httpContent;

        public HttpRequestMessageStub(string content)
        {
            _content = content;
            _httpContent = new HttpContentStub(content);

            var field = typeof(HttpRequestMessage).GetField(nameof(_content), BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(this, _httpContent);
        }
    }

    internal class HttpContentStub : HttpContent
    {
        private readonly string _content;
        private readonly MemoryStream _stream;

        public HttpContentStub(string content)
        {
            _content = content;
            _stream = new MemoryStream(Encoding.ASCII.GetBytes(content));

            var field = typeof(HttpContent).GetField("_bufferedContent", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(this, _stream);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) => throw new NotImplementedException();
        protected override bool TryComputeLength(out long length) => throw new NotImplementedException();
    }
}
