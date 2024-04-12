using Ocelot.Cache;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Request.Middleware;
using System.Reflection;
using System.Text;

namespace Ocelot.UnitTests.Cache;

public sealed class DefaultCacheKeyGeneratorTests : UnitTest, IDisposable
{
    private readonly ICacheKeyGenerator _cacheKeyGenerator;
    private readonly HttpRequestMessage _request;

    private const string verb = "GET";
    private const string url = "https://some.url/blah?abcd=123";
    private const string header = nameof(DefaultCacheKeyGeneratorTests);
    private const string headerName = "auth";

    public DefaultCacheKeyGeneratorTests()
    {
        _cacheKeyGenerator = new DefaultCacheKeyGenerator();

        _request = new HttpRequestMessage
        {
            Method = new HttpMethod(verb),
            RequestUri = new Uri(url),
        };
        _request.Headers.Add(headerName, header);
    }

    [Fact]
    public void should_generate_cache_key_with_request_content()
    {
        const string noHeader = null;
        const string content = nameof(should_generate_cache_key_with_request_content);
        var cachekey = MD5Helper.GenerateMd5($"{verb}-{url}-{content}");
        CacheOptions options = new CacheOptions(100, "region", noHeader, true);

        this.Given(x => x.GivenDownstreamRoute(options))
            .And(x => GivenHasContent(content))
            .When(x => x.WhenGenerateRequestCacheKey())
            .Then(x => x.ThenGeneratedCacheKeyIs(cachekey))
            .BDDfy();
    }

    [Fact]
    public void should_generate_cache_key_without_request_content()
    {
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
        CacheOptions options = new CacheOptions(100, "region", headerName, true);
        var cachekey = MD5Helper.GenerateMd5($"{verb}-{url}-{header}-{content}");

        this.Given(x => x.GivenDownstreamRoute(options))
            .And(x => GivenHasContent(content))
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

    private void GivenHasContent(string content)
    {
        _request.Content = new StringContent(content);
    }

    private string _generatedCacheKey;

    private async Task WhenGenerateRequestCacheKey()
    {
        _generatedCacheKey = await _cacheKeyGenerator.GenerateRequestCacheKey(new DownstreamRequest(_request), _downstreamRoute);
    }

    private void ThenGeneratedCacheKeyIs(string expected)
    {
        _generatedCacheKey.ShouldBe(expected);
    }

    public void Dispose()
    {
        _request.Dispose();
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
