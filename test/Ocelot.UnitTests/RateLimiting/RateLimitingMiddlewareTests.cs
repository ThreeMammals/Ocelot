#if NET7_0_OR_GREATER
using Microsoft.AspNetCore.RateLimiting;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.RateLimiting;
using Ocelot.Request.Middleware;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using _DownstreamRouteHolder_ = Ocelot.DownstreamRouteFinder.DownstreamRouteHolder;
using _RateLimiting_ = Ocelot.RateLimiting.RateLimiting;

namespace Ocelot.UnitTests.RateLimiting;

public class RateLimitingMiddlewareTests : UnitTest
{
    private readonly IRateLimitStorage _storage;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly Mock<IHttpContextAccessor> _contextAccessor;
    private readonly RateLimitingMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly IRateLimiting _rateLimiting;
    private readonly List<DownstreamResponse> _downstreamResponses;
    private readonly string _url;
    private Func<string> _loggerMessage;

    public RateLimitingMiddlewareTests()
    {
        _url = "http://localhost:" + PortFinder.GetRandomPort();
        var cacheEntryOptions = new MemoryCacheOptions();
        _storage = new MemoryCacheRateLimitStorage(new MemoryCache(cacheEntryOptions));
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _logger.Setup(x => x.LogInformation(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(f => _loggerMessage = f);
        _loggerFactory.Setup(x => x.CreateLogger<RateLimitingMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _rateLimiting = new _RateLimiting_(_storage);
        _contextAccessor = new Mock<IHttpContextAccessor>();
        _middleware = new RateLimitingMiddleware(_next, _loggerFactory.Object, _rateLimiting, _contextAccessor.Object);
        _downstreamResponses = new();
    }

    [Fact]
    [Trait("Feat", "37")]
    public async Task Should_call_middleware_and_ratelimiting()
    {
        // Arrange
        const long limit = 3L;
        var downstreamRoute = GivenDownstreamRoute(rule: new("1s", "100s", limit));
        var route = GivenRoute(downstreamRoute);
        var dsHolder = new _DownstreamRouteHolder_(new(), route);

        // Act, Assert
        await WhenICallTheMiddlewareMultipleTimes(limit, dsHolder);
        _downstreamResponses.ForEach(dsr => dsr.ShouldBeNull());

        // Act, Assert: the next request should fail
        await WhenICallTheMiddlewareMultipleTimes(3, dsHolder);
        _downstreamResponses.ShouldNotBeNull();
        for (int i = 0; i < _downstreamResponses.Count; i++)
        {
            var response = _downstreamResponses[i].ShouldNotBeNull();
            response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests, $"Downstream Response no is {i}");
            var body = await response.Content.ReadAsStringAsync();
            body.ShouldBe("Exceeding!");
        }
    }

    [Theory]
    [Trait("Feat", "37")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_not_call_middleware_with_disabled_ratelimiting(bool hasOptions)
    {
        // Arrange
        RateLimitOptions options = hasOptions ? new(false) : null;
        var downstreamRoute = new DownstreamRouteBuilder()
                .WithRateLimitOptions(options)
                .Build();
        var route = GivenRoute(downstreamRoute);
        var dsHolder = new _DownstreamRouteHolder_(new(), route);

        // Act
        var contexts = await WhenICallTheMiddlewareMultipleTimes(1, dsHolder);

        // Assert
        _downstreamResponses.ShouldNotBeNull();
        _downstreamResponses.ForEach(dsr => dsr.ShouldBeNull());
        ShouldLogInformation("Rate limiting is disabled for route '?' via the EnableRateLimiting option.");
    }

    [Fact]
    [Trait("Feat", "37")]
    public async Task Should_call_middleware_with_whitelisted_client()
    {
        // Arrange
        var opts = GivenRateLimitOptions(new("1s", "100s", 3));
        var options = new RateLimitOptions(opts)
        {
            ClientWhitelist = ["ocelotclient2"],
        };
        var downstreamRoute = GivenDownstreamRoute(options);
        var route = GivenRoute(downstreamRoute);
        var dsHolder = new _DownstreamRouteHolder_(new(), route);

        // Act
        await WhenICallTheMiddlewareWithWhiteClient(dsHolder);

        // Assert
        _downstreamResponses.ForEach(dsr => dsr.ShouldBeNull());
    }

    [Fact]
    [Trait("Bug", "1305 ")] // https://github.com/ThreeMammals/Ocelot/issues/1305
    [Trait("PR", "1307 ")] // https://github.com/ThreeMammals/Ocelot/pull/1307
    public async Task ShouldPopulateRateLimitingHeaders()
    {
        // Arrange
        RateLimitOptions options = new()
        {
            EnableHeaders = true,
            ClientIdHeader = "ClientId",
            Rule = new("1s", "1s", 3),
        };
        var downstreamRoute = GivenDownstreamRoute(options);
        var route = GivenRoute(downstreamRoute);
        var dsHolder = new _DownstreamRouteHolder_(new(), route);
        var originalContext = new DefaultHttpContext();
        _contextAccessor.SetupGet(x => x.HttpContext).Returns(originalContext);

        // Act
        var contexts = await WhenICallTheMiddlewareMultipleTimes(1, dsHolder, null, originalContext);

        // Assert
        originalContext.Response.ShouldNotBeNull();
        _logger.Verify(x => x.LogInformation(It.IsAny<Func<string>>()), Times.Once);
        var msg = _loggerMessage.ShouldNotBeNull().Invoke();
        msg.ShouldStartWith("Route '?' must return rate limiting headers with the following data: 2/3 resets at "); // Route '?' must return rate limiting headers with the following data: 2/3 resets at 2025-09-11T13:37:13.7973731Z
    }

    [Theory]
    [Trait("Bug", "1305 ")]
    [Trait("PR", "1307 ")]
    [InlineData(false, false, 0)]
    [InlineData(false, true, 0)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 1)]
    public async Task ShouldPopulateRateLimitingHeaders_Branches(bool enableHeaders, bool hasContext, int loggedTimes)
    {
        // Arrange
        RateLimitOptions options = new()
        {
            EnableHeaders = enableHeaders,
            ClientIdHeader = "ClientId",
            Rule = new("1s", "1s", 3),
        };
        var downstreamRoute = GivenDownstreamRoute(options);
        var route = GivenRoute(downstreamRoute);
        var dsHolder = new _DownstreamRouteHolder_(new(), route);
        var originalContext = hasContext ? new DefaultHttpContext() : null;
        _contextAccessor.SetupGet(x => x.HttpContext).Returns(originalContext);

        // Act
        var contexts = await WhenICallTheMiddlewareMultipleTimes(1, dsHolder, null, originalContext);

        // Assert
        _logger.Verify(x => x.LogInformation(It.IsAny<Func<string>>()), Times.Exactly(loggedTimes));
    }

    [Fact]
    [Trait("Bug", "1305 ")]
    [Trait("PR", "1307 ")]
    public async Task SetRateLimitHeaders()
    {
        // Arrange
        var today = new DateTime(2025, 9, 11, 3, 4, 5, 6, 7, DateTimeKind.Utc);
        var context = new DefaultHttpContext();
        var state = new RateLimitHeaders(context, 3, 2, today);
        var method = _middleware.GetType().GetMethod("SetRateLimitHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

        // Act
        Task t = method.Invoke(_middleware, [state]) as Task;
        await t;

        // Assert
        Assert.True(t.IsCompleted);
        var headers = context.Response.Headers;
        Assert.NotEmpty(headers);
        Assert.True(headers.ContainsKey(RateLimitingHeaders.X_RateLimit_Limit));
        Assert.True(headers.ContainsKey(RateLimitingHeaders.X_RateLimit_Remaining));
        Assert.True(headers.ContainsKey(RateLimitingHeaders.X_RateLimit_Reset));
        Assert.Equal("3", headers[RateLimitingHeaders.X_RateLimit_Limit]);
        Assert.Equal("2", headers[RateLimitingHeaders.X_RateLimit_Remaining]);
        Assert.Equal("2025-09-11T03:04:05.0060070Z", headers[RateLimitingHeaders.X_RateLimit_Reset]);
    }

    [Fact]
    [Trait("Bug", "1590")]
    public async Task Invoke_PeriodTimespanValueIsGreaterThanPeriod_StatusNotEqualTo429()
    {
        // Arrange
        const long limit = 100L;
        var rule = new RateLimitRule("1s", "30s", limit); // bug scenario
        var downstreamRoute = GivenDownstreamRoute(rule: rule);
        var route = GivenRoute(downstreamRoute);
        var dsHolder = new _DownstreamRouteHolder_(new(), route);

        // Act, Assert: 100 requests must be successful
        var contexts = await WhenICallTheMiddlewareMultipleTimes(limit, dsHolder); // make 100 requests, but not exceed the limit
        _downstreamResponses.ForEach(dsr => dsr.ShouldBeNull());
        contexts.ForEach(ctx =>
        {
            ctx.ShouldNotBeNull();
            ctx.Items.Errors().ShouldNotBeNull().ShouldBeEmpty(); // no errors
            ctx.Response.StatusCode.ShouldBe((int)HttpStatusCode.OK); // not 429 aka TooManyRequests
        });

        // Act, Assert: the next 101st request should fail
        contexts = await WhenICallTheMiddlewareMultipleTimes(1, dsHolder);
        _downstreamResponses.ShouldNotBeNull();
        var ds = _downstreamResponses.SingleOrDefault().ShouldNotBeNull();
        ds.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests, $"Downstream Response no {limit + 1}");
        var body = await ds.Content.ReadAsStringAsync();
        body.ShouldBe("Exceeding!");
        contexts[0].Items.Errors().ShouldNotBeNull().ShouldNotBeEmpty(); // having errors
        contexts[0].Items.Errors().Single().HttpStatusCode.ShouldBe((int)HttpStatusCode.TooManyRequests);
    }

    [Fact]
    [Trait("Feat", "37")]
    [Trait("Feat", "585")]
    [Trait("PR", "2294")]
    public async Task Invoke_NoClientHeader_Status503_ShouldLogWarning()
    {
        // Arrange
        var downstreamRoute = GivenDownstreamRoute();
        var route = GivenRoute(downstreamRoute);
        var dsHolder = new _DownstreamRouteHolder_(new(), route);

        // Act
        var contexts = await WhenICallTheMiddlewareMultipleTimes(1, dsHolder, "bla-bla-header:spy");

        // Assert
        var ctx = contexts[0].ShouldNotBeNull();
        var errors = ctx.Items.Errors().ShouldNotBeNull();
        var err = Assert.Single(errors);
        Assert.IsType<QuotaExceededError>(err);
        Assert.Equal("Rate limiting client could not be identified for the route '?' due to a missing or unknown client ID header required by rule '3/1s/w1s'!", err.Message);
        var ds = _downstreamResponses.SingleOrDefault().ShouldNotBeNull();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ds.StatusCode);
        var body = await ds.Content.ReadAsStringAsync();
        Assert.Equal("Rate limiting client could not be identified for the route '?' due to a missing or unknown client ID header required by rule '3/1s/w1s'!", body);
        _logger.Verify(x => x.LogWarning(err.Message), Times.Once);
    }

#if NET7_0_OR_GREATER
    [Fact]
    [Trait("Feat", "2138")]
    public async Task Should_add_EnableRateLimittingAttribute_When_AspNetRateLimiting()
    {
        // Arrange
        const long limit = 3L;
        var upstreamTemplate = new UpstreamPathTemplateBuilder()
            .Build();
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithEnableRateLimiting(true)
            .WithRateLimitOptions(new(
                enableRateLimiting: true,
                clientIdHeader: null,
                getClientWhitelist: null,
                disableRateLimitHeaders: false,
                quotaExceededMessage: null,
                rateLimitCounterPrefix: null,
                null,
                (int)HttpStatusCode.TooManyRequests,
                "testPolicy"))
            .WithUpstreamHttpMethod(new() { "Get" })
            .WithUpstreamPathTemplate(upstreamTemplate)
            .Build();
        var route = new RouteBuilder()
            .WithDownstreamRoute(downstreamRoute)
            .WithUpstreamHttpMethod(new() { "Get" })
            .Build();
        var downstreamRouteHolder = new _DownstreamRouteHolder_(new(), route);

        // Act, Assert
        var contexts = await WhenICallTheMiddlewareMultipleTimes(limit+1, downstreamRouteHolder);
        _downstreamResponses.ForEach(dsr => dsr.ShouldBeNull());
        
        contexts.ForEach(ctx =>
        {
            var endpoint = ctx.GetEndpoint();
            endpoint.ShouldNotBeNull();
            
            var rateLimitAttribute = endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>();
            rateLimitAttribute.PolicyName.ShouldBe("testPolicy");
        });
        
    }
#endif

    private static RateLimitOptions GivenRateLimitOptions(RateLimitRule rule = null, [CallerMemberName] string testName = null) => new(
            enableRateLimiting: true,
            clientIdHeader: "ClientId",
            clientWhitelist: [],
            enableHeaders: true,
            quotaExceededMessage: "Exceeding!",
            rateLimitCounterPrefix: testName,
            rule ?? new("1s", "1s", 3),
            StatusCodes.Status429TooManyRequests);

    private static DownstreamRoute GivenDownstreamRoute(RateLimitOptions options = null, RateLimitRule rule = null, [CallerMemberName] string testName = null)
        => new DownstreamRouteBuilder()
        .WithRateLimitOptions(options ?? GivenRateLimitOptions(rule))
        .WithUpstreamHttpMethod([HttpMethods.Get])
        .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().Build())
        .WithLoadBalancerKey(testName)
        .Build();

    private static Route GivenRoute(DownstreamRoute dr) => new RouteBuilder()
        .WithDownstreamRoute(dr)
        .WithUpstreamHttpMethod([HttpMethods.Get])
        .Build();

    private async Task<List<HttpContext>> WhenICallTheMiddlewareMultipleTimes(long times, _DownstreamRouteHolder_ holder, string header = null, HttpContext originalContext = null)
    {
        var contexts = new List<HttpContext>();
        _downstreamResponses.Clear();
        for (var i = 0; i < times; i++)
        {
            var context = originalContext ?? new DefaultHttpContext();
            var stream = GetFakeStream($"{i}");
            context.Response.Body = stream;
            context.Response.RegisterForDispose(stream);
            context.Items.UpsertDownstreamRoute(holder.Route.DownstreamRoute[0]);
            context.Items.UpsertTemplatePlaceholderNameAndValues(holder.TemplatePlaceholderNameAndValues);
            context.Items.UpsertDownstreamRoute(holder);
            var request = new HttpRequestMessage(new HttpMethod("GET"), _url);
            context.Items.UpsertDownstreamRequest(new DownstreamRequest(request));
            header ??= "ClientId:ocelotclient1";
            var hdr = header.Split(':');
            context.Request.Headers.TryAdd(hdr[0], hdr[1]);
            contexts.Add(context);

            await _middleware.Invoke(context);

            _downstreamResponses.Add(context.Items.DownstreamResponse());
        }

        return contexts;
    }

    private static MemoryStream GetFakeStream(string str)
    {
        byte[] data = Encoding.ASCII.GetBytes(str);
        return new MemoryStream(data, 0, data.Length);
    }

    private async Task WhenICallTheMiddlewareWithWhiteClient(_DownstreamRouteHolder_ holder)
    {
        const string ClientId = "ocelotclient2";
        for (var i = 0; i < 10; i++)
        {
            var context = new DefaultHttpContext();
            var stream = GetFakeStream($"{i}");
            context.Response.Body = stream;
            context.Response.RegisterForDispose(stream);
            context.Items.UpsertDownstreamRoute(holder.Route.DownstreamRoute[0]);
            context.Items.UpsertTemplatePlaceholderNameAndValues(holder.TemplatePlaceholderNameAndValues);
            context.Items.UpsertDownstreamRoute(holder);
            var request = new HttpRequestMessage(new HttpMethod("GET"), _url);
            request.Headers.Add("ClientId", ClientId);
            context.Items.UpsertDownstreamRequest(new DownstreamRequest(request));
            context.Request.Headers.TryAdd("ClientId", ClientId);

            await _middleware.Invoke(context);

            _downstreamResponses.Add(context.Items.DownstreamResponse());
        }
    }

    private void ShouldLogInformation(string expected)
    {
        _logger.Verify(x => x.LogInformation(It.IsAny<Func<string>>()), Times.Once);
        var msg = _loggerMessage.ShouldNotBeNull().Invoke();
        msg.ShouldBe(expected);
    }
}
