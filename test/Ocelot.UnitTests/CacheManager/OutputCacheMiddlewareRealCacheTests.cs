using CacheManager.Core;
using Microsoft.AspNetCore.Http;
using Ocelot.Cache;
using Ocelot.Cache.CacheManager;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Ocelot.UnitTests.CacheManager;

public class OutputCacheMiddlewareRealCacheTests : UnitTest
{
    private readonly OcelotCacheManagerCache<CachedResponse> _cacheManager;
    private readonly ICacheKeyGenerator _cacheKeyGenerator;
    private readonly OutputCacheMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly DefaultHttpContext _httpContext;

    public OutputCacheMiddlewareRealCacheTests()
    {
        _httpContext = new DefaultHttpContext();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<OutputCacheMiddleware>()).Returns(_logger.Object);
        var cacheManagerOutputCache = CacheFactory.Build<CachedResponse>("OcelotOutputCache", x =>
        {
            x.WithDictionaryHandle();
        });
        _cacheManager = new OcelotCacheManagerCache<CachedResponse>(cacheManagerOutputCache);
        _cacheKeyGenerator = new DefaultCacheKeyGenerator();
        _httpContext.Items.UpsertDownstreamRequest(new Ocelot.Request.Middleware.DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123")));
        _next = context => Task.CompletedTask;
        _middleware = new OutputCacheMiddleware(_next, _loggerFactory.Object, _cacheManager, _cacheKeyGenerator);
    }

    [Fact]
    public async Task Should_cache_content_headers()
    {
        // Arrange
        var content = new StringContent("{\"Test\": 1}")
        {
            Headers = { ContentType = new MediaTypeHeaderValue("application/json") },
        };
        var response = new DownstreamResponse(content, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "fooreason");
        GivenResponseIsNotCached(response);
        GivenTheDownstreamRouteIs(null);

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheContentTypeHeaderIsCached();
    }

    [Theory]
    [InlineData(null, HttpStatusCode.OK)]
    [InlineData(null, HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.InternalServerError)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }, HttpStatusCode.OK)]
    [InlineData(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }, HttpStatusCode.Forbidden)]
    public async Task Should_cache_when_whitelisted(HttpStatusCode[] statusCodes, HttpStatusCode responseCode)
    {
        // Arrange
        var content = new StringContent("{\"Test\": 1}")
        {
            Headers = { ContentType = new MediaTypeHeaderValue("application/json") },
        };
        var response = new DownstreamResponse(content, responseCode, new List<KeyValuePair<string, IEnumerable<string>>>(), "fooreason");
        GivenResponseIsNotCached(response);
        GivenTheDownstreamRouteIs(new CacheOptions(100, "kanken", null, false, statusCodes));

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheContentTypeHeaderIsCached();
    }

    [Theory]
    [InlineData(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }, HttpStatusCode.InternalServerError)]
    [InlineData(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }, HttpStatusCode.BadRequest)]
    public async Task Should_not_cache_when_not_whitelisted(HttpStatusCode[] statusCodes, HttpStatusCode responseCode)
    {
        // Arrange
        var content = new StringContent("{\"Test\": 1}")
        {
            Headers = { ContentType = new MediaTypeHeaderValue("application/json") },
        };
        var response = new DownstreamResponse(content, responseCode, new List<KeyValuePair<string, IEnumerable<string>>>(), "fooreason");
        GivenResponseIsNotCached(response);
        GivenTheDownstreamRouteIs(new CacheOptions(100, "kanken", null, false, statusCodes));

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheResponseIsNotCached();
    }

    private async Task WhenICallTheMiddleware()
    {
        await _middleware.Invoke(_httpContext);
    }

    private void ThenTheContentTypeHeaderIsCached()
    {
        var cacheKey = MD5Helper.GenerateMd5("GET-https://some.url/blah?abcd=123-"); // absent header -> '-' dash char is added at the end
        var result = _cacheManager.Get(cacheKey, "kanken");
        var header = result.ContentHeaders["Content-Type"];
        header.First().ShouldBe("application/json");
    }

    private void ThenTheResponseIsNotCached()
    {
        var cacheKey = MD5Helper.GenerateMd5("GET-https://some.url/blah?abcd=123-"); // absent header -> '-' dash char is added at the end
        var result = _cacheManager.Get(cacheKey, "kanken");
        Assert.Null(result);
    }

    private void GivenResponseIsNotCached(DownstreamResponse response)
    {
        _httpContext.Items.UpsertDownstreamResponse(response);
    }

    private void GivenTheDownstreamRouteIs(CacheOptions options)
    {
        var route = new DownstreamRouteBuilder()
            .WithCacheOptions(options ?? new CacheOptions(100, "kanken", null, false, null))
            .WithUpstreamHttpMethod(new List<string> { "Get" })
            .Build();

        _httpContext.Items.UpsertDownstreamRoute(route);
    }
}
