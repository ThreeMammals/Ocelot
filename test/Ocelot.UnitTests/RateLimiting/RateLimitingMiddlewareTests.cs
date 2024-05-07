using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.RateLimiting;
using Ocelot.RateLimiting.Middleware;
using Ocelot.Request.Middleware;
using System.Text;
using _DownstreamRouteHolder_ = Ocelot.DownstreamRouteFinder.DownstreamRouteHolder;
using _RateLimiting_ = Ocelot.RateLimiting.RateLimiting;

namespace Ocelot.UnitTests.RateLimiting;

public class RateLimitingMiddlewareTests : UnitTest
{
    private readonly IRateLimitStorage _storage;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly RateLimitingMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly IRateLimiting _rateLimiting;
    private readonly List<DownstreamResponse> _downstreamResponses;
    private readonly string _url;

    public RateLimitingMiddlewareTests()
    {
        _url = "http://localhost:51879";
        var cacheEntryOptions = new MemoryCacheOptions();
        _storage = new MemoryCacheRateLimitStorage(new MemoryCache(cacheEntryOptions));
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<RateLimitingMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _rateLimiting = new _RateLimiting_(_storage);
        _middleware = new RateLimitingMiddleware(_next, _loggerFactory.Object, _rateLimiting);
        _downstreamResponses = new();
    }

    [Fact]
    [Trait("Feat", "37")]
    public async Task Should_call_middleware_and_ratelimiting()
    {
        // Arrange
        const long limit = 3L;
        var upstreamTemplate = new UpstreamPathTemplateBuilder()
            .Build();
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithEnableRateLimiting(true)
            .WithRateLimitOptions(new(
                enableRateLimiting: true,
                clientIdHeader: "ClientId",
                getClientWhitelist: () => new List<string>(),
                disableRateLimitHeaders: false,
                quotaExceededMessage: "Exceeding!",
                rateLimitCounterPrefix: string.Empty,
                new RateLimitRule("1s", 100.0D, limit),
                (int)HttpStatusCode.TooManyRequests))
            .WithUpstreamHttpMethod(new() { "Get" })
            .WithUpstreamPathTemplate(upstreamTemplate)
            .Build();
        var route = new RouteBuilder()
            .WithDownstreamRoute(downstreamRoute)
            .WithUpstreamHttpMethod(new() { "Get" })
            .Build();
        var downstreamRouteHolder = new _DownstreamRouteHolder_(new(), route);

        // Act, Assert
        await WhenICallTheMiddlewareMultipleTimes(limit, downstreamRouteHolder);
        _downstreamResponses.ForEach(dsr => dsr.ShouldBeNull());

        // Act, Assert: the next request should fail
        await WhenICallTheMiddlewareMultipleTimes(3, downstreamRouteHolder);
        _downstreamResponses.ShouldNotBeNull();
        for (int i = 0; i < _downstreamResponses.Count; i++)
        {
            var response = _downstreamResponses[i].ShouldNotBeNull();
            response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests, $"Downstream Response no is {i}");
            var body = await response.Content.ReadAsStringAsync();
            body.ShouldBe("Exceeding!");
        }
    }

    [Fact]
    [Trait("Feat", "37")]
    public async Task Should_call_middleware_withWhitelistClient()
    {
        // Arrange
        var route = new RouteBuilder()
            .WithDownstreamRoute(new DownstreamRouteBuilder()
                .WithEnableRateLimiting(true)
                .WithRateLimitOptions(new(
                    enableRateLimiting: true,
                    clientIdHeader: "ClientId",
                    getClientWhitelist: () => new List<string> { "ocelotclient2" },
                    disableRateLimitHeaders: false,
                    quotaExceededMessage: "Exceeding!",
                    rateLimitCounterPrefix: string.Empty,
                    new RateLimitRule("1s", 100.0D, 3),
                    (int)HttpStatusCode.TooManyRequests))
                .WithUpstreamHttpMethod(new() { "Get" })
                .Build())
            .WithUpstreamHttpMethod(new() { "Get" })
            .Build();
        var downstreamRoute = new _DownstreamRouteHolder_(new(), route);

        // Act
        await WhenICallTheMiddlewareWithWhiteClient(downstreamRoute);

        // Assert
        _downstreamResponses.ForEach(dsr => dsr.ShouldBeNull());
    }

    [Fact]
    [Trait("Bug", "1590")]
    public async Task MiddlewareInvoke_PeriodTimespanValueIsGreaterThanPeriod_StatusNotEqualTo429()
    {
        // Arrange
        const long limit = 100L;
        var upstreamTemplate = new UpstreamPathTemplateBuilder()
            .Build();
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithEnableRateLimiting(true)
            .WithRateLimitOptions(new(
                enableRateLimiting: true,
                clientIdHeader: "ClientId",
                getClientWhitelist: () => new List<string>(),
                disableRateLimitHeaders: false,
                quotaExceededMessage: "Exceeding!",
                rateLimitCounterPrefix: string.Empty,
                new RateLimitRule("1s", 30.0D, limit), // bug scenario
                (int)HttpStatusCode.TooManyRequests))
            .WithUpstreamHttpMethod(new() { "Get" })
            .WithUpstreamPathTemplate(upstreamTemplate)
            .Build();
        var route = new RouteBuilder()
            .WithDownstreamRoute(downstreamRoute)
            .WithUpstreamHttpMethod(new() { "Get" })
            .Build();
        var downstreamRouteHolder = new _DownstreamRouteHolder_(new(), route);

        // Act, Assert: 100 requests must be successful
        var contexts = await WhenICallTheMiddlewareMultipleTimes(limit, downstreamRouteHolder); // make 100 requests, but not exceed the limit
        _downstreamResponses.ForEach(dsr => dsr.ShouldBeNull());
        contexts.ForEach(ctx =>
        {
            ctx.ShouldNotBeNull();
            ctx.Items.Errors().ShouldNotBeNull().ShouldBeEmpty(); // no errors
            ctx.Response.StatusCode.ShouldBe((int)HttpStatusCode.OK); // not 429 aka TooManyRequests
        });

        // Act, Assert: the next 101st request should fail
        contexts = await WhenICallTheMiddlewareMultipleTimes(1, downstreamRouteHolder);
        _downstreamResponses.ShouldNotBeNull();
        var ds = _downstreamResponses.SingleOrDefault().ShouldNotBeNull();
        ds.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests, $"Downstream Response no {limit + 1}");
        var body = await ds.Content.ReadAsStringAsync();
        body.ShouldBe("Exceeding!");
        contexts[0].Items.Errors().ShouldNotBeNull().ShouldNotBeEmpty(); // having errors
        contexts[0].Items.Errors().Single().HttpStatusCode.ShouldBe((int)HttpStatusCode.TooManyRequests);
    }

    private async Task<List<HttpContext>> WhenICallTheMiddlewareMultipleTimes(long times, _DownstreamRouteHolder_ downstreamRoute)
    {
        var contexts = new List<HttpContext>();
        _downstreamResponses.Clear();
        for (var i = 0; i < times; i++)
        {
            var context = new DefaultHttpContext();
            var stream = GetFakeStream($"{i}");
            context.Response.Body = stream;
            context.Response.RegisterForDispose(stream);
            context.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
            context.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);
            context.Items.UpsertDownstreamRoute(downstreamRoute);
            var request = new HttpRequestMessage(new HttpMethod("GET"), _url);
            context.Items.UpsertDownstreamRequest(new DownstreamRequest(request));
            context.Request.Headers.TryAdd("ClientId", "ocelotclient1");
            contexts.Add(context);

            await _middleware.Invoke(context);

            _downstreamResponses.Add(context.Items.DownstreamResponse());
        }

        return contexts;
    }

    private static Stream GetFakeStream(string str)
    {
        byte[] data = Encoding.ASCII.GetBytes(str);
        return new MemoryStream(data, 0, data.Length);
    }

    private async Task WhenICallTheMiddlewareWithWhiteClient(_DownstreamRouteHolder_ downstreamRoute)
    {
        const string ClientId = "ocelotclient2";
        for (var i = 0; i < 10; i++)
        {
            var context = new DefaultHttpContext();
            var stream = GetFakeStream($"{i}");
            context.Response.Body = stream;
            context.Response.RegisterForDispose(stream);
            context.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
            context.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);
            context.Items.UpsertDownstreamRoute(downstreamRoute);
            var request = new HttpRequestMessage(new HttpMethod("GET"), _url);
            request.Headers.Add("ClientId", ClientId);
            context.Items.UpsertDownstreamRequest(new DownstreamRequest(request));
            context.Request.Headers.TryAdd("ClientId", ClientId);

            await _middleware.Invoke(context);

            _downstreamResponses.Add(context.Items.DownstreamResponse());
        }
    }
}
