using Microsoft.AspNetCore.Http;
using Ocelot.Cache;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Filter;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester;

namespace Ocelot.UnitTests.Cache;

public class OutputCacheMiddlewareTests : UnitTest
{
    private readonly Mock<IOcelotCache<CachedResponse>> _cache = new();
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory = new();
    private readonly Mock<IOcelotLogger> _logger = new();
    private readonly Mock<ICacheKeyGenerator> _cacheGenerator = new();
    private OutputCacheMiddleware _middleware;
    private RequestDelegate _next;
    private readonly ICacheKeyGenerator _cacheKeyGenerator;
    private CachedResponse _response;
    private readonly DefaultHttpContext _httpContext;
    Func<string> _message;

    public OutputCacheMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _cacheKeyGenerator = new DefaultCacheKeyGenerator();
        _loggerFactory.Setup(x => x.CreateLogger<OutputCacheMiddleware>()).Returns(_logger.Object);
        _logger.Setup(x => x.LogDebug(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(m => _message = m);
        _cacheGenerator.Setup(x => x.GenerateRequestCacheKey(It.IsAny<DownstreamRequest>(), It.IsAny<DownstreamRoute>()))
            .Returns<DownstreamRequest, DownstreamRoute>((req, rou) => _cacheKeyGenerator.GenerateRequestCacheKey(req, rou));
        _next = context => Task.CompletedTask;
        _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123")));
    }

    [Fact]
    public async Task Should_returned_cached_item_when_it_is_in_cache()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "test", new List<string> { "test" } },
        };
        var contentHeaders = new Dictionary<string, IEnumerable<string>>
        {
            { "content-type", new List<string> { "application/json" } },
        };
        var cachedResponse = new CachedResponse(HttpStatusCode.OK, headers, string.Empty, contentHeaders, "some reason");
        GivenThereIsACachedResponse(cachedResponse);
        GivenTheDownstreamRouteIs();

        // Act
        await WhenICallTheMiddlewareAsync();

        // Assert
        ThenTheCacheGetIsCalledCorrectly();
    }

    [Fact]
    public async Task Should_returned_cached_item_when_it_is_in_cache_expires_header()
    {
        // Arrange
        var contentHeaders = new Dictionary<string, IEnumerable<string>>
        {
            { "Expires", new List<string> { "-1" } },
        };
        var cachedResponse = new CachedResponse(HttpStatusCode.OK, new Dictionary<string, IEnumerable<string>>(), string.Empty, contentHeaders, "some reason");
        GivenThereIsACachedResponse(cachedResponse);
        GivenTheDownstreamRouteIs();

        // Act
        await WhenICallTheMiddlewareAsync();

        // Assert
        ThenTheCacheGetIsCalledCorrectly();
    }

    [Fact]
    public async Task Should_continue_with_pipeline_and_cache_response()
    {
        // Arrange
        GivenResponseIsNotCached(new HttpResponseMessage());
        GivenTheDownstreamRouteIs();

        // Act
        await WhenICallTheMiddlewareAsync();

        // Assert
        ThenTheCacheAddIsCalled();
    }

    [Fact]
    public async Task Should_not_use_cache()
    {
        // Arrange
        GivenResponseIsNotCached(new HttpResponseMessage());
        GivenTheDownstreamRouteIs(new CacheOptions(0, null, null, null));

        // Act
        await WhenICallTheMiddlewareAsync();

        // Assert
        _cacheGenerator.Verify(
            x => x.GenerateRequestCacheKey(It.IsAny<DownstreamRequest>(), It.IsAny<DownstreamRoute>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_not_add_to_cache_when_errors()
    {
        // Arrange
        GivenResponseIsNotCached(new HttpResponseMessage());
        GivenTheDownstreamRouteIs();
        _next = static context =>
        {
            context.Items.UpsertErrors([new RequestCanceledError("Bla-bla message")]);
            return Task.CompletedTask;
        };

        // Act
        await WhenICallTheMiddlewareAsync();

        // Assert
        ThenTheCacheAddIsCalled(Times.Never);
        ThenTheMessageIs("There was a pipeline error for the 'GET-https://some.url/blah?abcd=123' key.");
    }

    [Fact]
    public async Task CreateHttpResponseMessage_CachedIsNull()
    {
        // Arrange
        CachedResponse cached = null;
        GivenThereIsACachedResponse(cached);
        GivenTheDownstreamRouteIs();

        // Act
        await WhenICallTheMiddlewareAsync();

        // Assert
        ThenTheCacheGetIsCalledCorrectly();
    }

    private void ThenTheMessageIs(string expected)
    {
        Assert.NotNull(_message);
        var msg = _message.Invoke();
        Assert.Equal(expected, msg);
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
        var response = new HttpResponseMessage(responseCode);
        GivenResponseIsNotCached(response);
        GivenTheDownstreamRouteIs(new CacheOptions(100, "kanken", null, false, new HttpStatusCodeFilter(FilterType.Whitelist, statusCodes)));

        // Act
        await WhenICallTheMiddlewareAsync();

        // Assert
        ThenTheCacheAddIsCalled(Times.Once);
    }

    [Theory]
    [InlineData(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }, HttpStatusCode.BadRequest)]
    [InlineData(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }, HttpStatusCode.InternalServerError)]
    public async Task Should_not_cache_when_not_whitelisted(HttpStatusCode[] statusCodes, HttpStatusCode responseCode)
    {
        // Arrange
        var response = new HttpResponseMessage(responseCode);
        GivenResponseIsNotCached(response);
        GivenTheDownstreamRouteIs(new CacheOptions(100, "kanken", null, false, new HttpStatusCodeFilter(FilterType.Whitelist, statusCodes)));

        // Act
        await WhenICallTheMiddlewareAsync();

        // Assert
        ThenTheCacheAddIsCalled(Times.Never);
    }

    [Theory]
    [InlineData(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }, HttpStatusCode.OK)]
    [InlineData(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }, HttpStatusCode.Forbidden)]
    public async Task Should_not_cache_when_blacklisted(HttpStatusCode[] statusCodes, HttpStatusCode responseCode)
    {
        // Arrange
        var response = new HttpResponseMessage(responseCode);
        GivenResponseIsNotCached(response);
        GivenTheDownstreamRouteIs(new CacheOptions(100, "kanken", null, false, new HttpStatusCodeFilter(FilterType.Blacklist, statusCodes)));

        // Act
        await WhenICallTheMiddlewareAsync();

        // Assert
        ThenTheCacheAddIsCalled(Times.Never);
    }

    [Theory]
    [InlineData(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }, HttpStatusCode.Unauthorized)]
    [InlineData(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }, HttpStatusCode.InternalServerError)]
    public async Task Should_cache_when_not_blacklisted(HttpStatusCode[] statusCodes, HttpStatusCode responseCode)
    {
        // Arrange
        var response = new HttpResponseMessage(responseCode);
        GivenResponseIsNotCached(response);
        GivenTheDownstreamRouteIs(new CacheOptions(100, "kanken", null, false, new HttpStatusCodeFilter(FilterType.Blacklist, statusCodes)));

        // Act
        await WhenICallTheMiddlewareAsync();

        // Assert
        ThenTheCacheAddIsCalled(Times.Once);
    }

    private async Task WhenICallTheMiddlewareAsync()
    {
        _middleware = new OutputCacheMiddleware(_next, _loggerFactory.Object, _cache.Object, _cacheGenerator.Object);
        await _middleware.Invoke(_httpContext);
    }

    private void GivenThereIsACachedResponse(CachedResponse response)
    {
        _response = response;
        _cache
          .Setup(x => x.Get(It.IsAny<string>(), It.IsAny<string>()))
          .Returns(_response);
    }

    private void GivenResponseIsNotCached(HttpResponseMessage responseMessage)
    {
        _httpContext.Items.UpsertDownstreamResponse(new DownstreamResponse(responseMessage));
    }

    private void GivenTheDownstreamRouteIs(CacheOptions options = null)
    {
        var downRoute = new DownstreamRouteBuilder()
            .WithCacheOptions(options ?? new(100, "kanken", null, false))
            .WithUpstreamHttpMethod([ "Get" ])
            .Build();
        var route = new Route(downRoute)
        {
            UpstreamHttpMethod = [HttpMethod.Get],
        };
        var downstreamRoute = new Ocelot.DownstreamRouteFinder.DownstreamRouteHolder(new List<PlaceholderNameAndValue>(), route);
        _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);
        _httpContext.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
    }

    private void ThenTheCacheGetIsCalledCorrectly()
    {
        _cache.Verify(
            x => x.Get(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    private void ThenTheCacheAddIsCalled(Func<Times> howMany = null)
    {
        _cache.Verify(
            x => x.Add(It.IsAny<string>(), It.IsAny<CachedResponse>(), It.IsAny<string>(), It.IsAny<TimeSpan>()),
            howMany ?? Times.Once);
    }
}
