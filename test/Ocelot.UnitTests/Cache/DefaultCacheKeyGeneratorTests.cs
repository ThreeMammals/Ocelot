using Microsoft.AspNetCore.Http;
using Ocelot.Cache;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Request.Middleware;
using System.Reflection;
using System.Text;

namespace Ocelot.UnitTests.Cache;

public sealed class DefaultCacheKeyGeneratorTests : UnitTest, IDisposable
{
    private readonly DefaultCacheKeyGenerator _generator;
    private readonly HttpRequestMessage _request;

    private readonly string verb = HttpMethods.Get;
    private const string url = "https://some.url/blah?abcd=123";
    private const string header = nameof(DefaultCacheKeyGeneratorTests);
    private const string headerName = "auth";

    public DefaultCacheKeyGeneratorTests()
    {
        _generator = new DefaultCacheKeyGenerator();
        _request = new HttpRequestMessage
        {
            Method = new(verb),
            RequestUri = new(url),
        };
        _request.Headers.Add(headerName, header);
    }

    [Fact]
    public async Task Should_generate_cache_key_with_request_content()
    {
        // Arrange
        const string noHeader = null;
        const string content = nameof(Should_generate_cache_key_with_request_content);
        var cachekey = MD5Helper.GenerateMd5($"{verb}-{url}--{content}");
        var options = new CacheOptions(100, "region", noHeader, true);
        var route = GivenDownstreamRoute(options);
        _request.Content = new StringContent(content);

        // Act
        var generatedCacheKey = await WhenGenerateRequestCacheKey(route);

        // Assert
        generatedCacheKey.ShouldBe(cachekey);
    }

    [Fact]
    public async Task Should_generate_cache_key_without_request_content()
    {
        // Arrange
        CacheOptions options = null;
        var cachekey = MD5Helper.GenerateMd5($"{verb}-{url}");
        var route = GivenDownstreamRoute(options);

        // Act
        var generatedCacheKey = await WhenGenerateRequestCacheKey(route);

        // Assert
        generatedCacheKey.ShouldBe(cachekey);
    }

    [Fact]
    public async Task Should_generate_cache_key_with_cache_options_header()
    {
        // Arrange
        var options = new CacheOptions(100, "region", headerName, false);
        var cachekey = MD5Helper.GenerateMd5($"{verb}-{url}-{header}");
        var route = GivenDownstreamRoute(options);

        // Act
        var generatedCacheKey = await WhenGenerateRequestCacheKey(route);

        // Assert
        generatedCacheKey.ShouldBe(cachekey);

        // Scenario 2: No header
        _request.Headers.Clear();
        cachekey = MD5Helper.GenerateMd5($"{verb}-{url}-");
        generatedCacheKey = await WhenGenerateRequestCacheKey(route);
        Assert.Equal(cachekey, generatedCacheKey);
    }

    [Fact]
    public async Task Should_generate_cache_key_happy_path()
    {
        // Arrange
        const string content = nameof(Should_generate_cache_key_happy_path);
        var options = new CacheOptions(100, "region", headerName, true);
        var cachekey = MD5Helper.GenerateMd5($"{verb}-{url}-{header}-{content}");
        var route = GivenDownstreamRoute(options);
        _request.Content = new StringContent(content);

        // Act
        var generatedCacheKey = await WhenGenerateRequestCacheKey(route);

        // Assert
        generatedCacheKey.ShouldBe(cachekey);
    }

    private static DownstreamRoute GivenDownstreamRoute(CacheOptions options) => new DownstreamRouteBuilder()
        .WithKey("key1")
        .WithCacheOptions(options)
        .Build();

    private ValueTask<string> WhenGenerateRequestCacheKey(DownstreamRoute route)
        => _generator.GenerateRequestCacheKey(new DownstreamRequest(_request), route);

    public void Dispose()
    {
        _request.Dispose();
    }
}

internal class HttpContentStub : HttpContent
{
    private readonly MemoryStream _stream;

    public HttpContentStub(string content)
    {
        _stream = new MemoryStream(Encoding.ASCII.GetBytes(content));

        var field = typeof(HttpContent).GetField("_bufferedContent", BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(this, _stream);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) => throw new NotImplementedException();
    protected override bool TryComputeLength(out long length) => throw new NotImplementedException();
}
