using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.RateLimiting;
using Ocelot.RateLimiting.Middleware;
using Ocelot.Request.Middleware;

namespace Ocelot.UnitTests.RateLimiting;

public class ClientRateLimitMiddlewareTests : UnitTest
{
    private readonly IRateLimitStorage _storage;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly ClientRateLimitMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly IRateLimitCore _rateLimitCore;
    private DownstreamResponse _downstreamResponse;
    private readonly string _url;

    public ClientRateLimitMiddlewareTests()
    {
        _url = "http://localhost:51879";
        var cacheEntryOptions = new MemoryCacheOptions();
        _storage = new MemoryCacheRateLimitStorage(new MemoryCache(cacheEntryOptions));
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<ClientRateLimitMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _rateLimitCore = new RateLimitCore(_storage);
        _middleware = new ClientRateLimitMiddleware(_next, _loggerFactory.Object, _rateLimitCore);
    }

    [Fact]
    public void Should_call_middleware_and_ratelimiting()
    {
        // Arrange
        var upstreamTemplate = new UpstreamPathTemplateBuilder()
            .Build();
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithEnableRateLimiting(true)
            .WithRateLimitOptions(new(
                enableRateLimiting: true,
                clientIdHeader: "ClientId",
                getClientWhitelist: () => new List<string>(),
                disableRateLimitHeaders: false,
                quotaExceededMessage: string.Empty,
                rateLimitCounterPrefix: string.Empty,
                new RateLimitRule("1s", 100.0D, 3),
                (int)HttpStatusCode.TooManyRequests))
            .WithUpstreamHttpMethod(new() { "Get" })
            .WithUpstreamPathTemplate(upstreamTemplate)
            .Build();
        var route = new RouteBuilder()
            .WithDownstreamRoute(downstreamRoute)
            .WithUpstreamHttpMethod(new() { "Get" })
            .Build();
        var downstreamRouteHolder = new Ocelot.DownstreamRouteFinder.DownstreamRouteHolder(new(), route);

        // Act, Assert
        WhenICallTheMiddlewareMultipleTimes(2, downstreamRouteHolder);
        _downstreamResponse.ShouldBeNull();

        // Act, Assert
        WhenICallTheMiddlewareMultipleTimes(3, downstreamRouteHolder);
        _downstreamResponse.ShouldNotBeNull()
            .StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public void Should_call_middleware_withWhitelistClient()
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
                    quotaExceededMessage: string.Empty,
                    rateLimitCounterPrefix: string.Empty,
                    new RateLimitRule("1s", 100.0D, 3),
                    (int)HttpStatusCode.TooManyRequests))
                .WithUpstreamHttpMethod(new() { "Get" })
                .Build())
            .WithUpstreamHttpMethod(new() { "Get" })
            .Build();
        var downstreamRoute = new Ocelot.DownstreamRouteFinder.DownstreamRouteHolder(new(), route);

        // Act
        WhenICallTheMiddlewareWithWhiteClient(downstreamRoute);

        // Assert
        _downstreamResponse.ShouldBeNull();
    }

    private void WhenICallTheMiddlewareMultipleTimes(int times, Ocelot.DownstreamRouteFinder.DownstreamRouteHolder downstreamRoute)
    {
        var contexts = new List<HttpContext>();
        for (var i = 0; i < times; i++)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new FakeStream();
            context.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
            context.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);
            context.Items.UpsertDownstreamRoute(downstreamRoute);
            var request = new HttpRequestMessage(new HttpMethod("GET"), _url);
            context.Items.UpsertDownstreamRequest(new DownstreamRequest(request));
            context.Request.Headers.TryAdd("ClientId", "ocelotclient1");
            contexts.Add(context);
        }

        foreach (var ctx in contexts)
        {
            _middleware.Invoke(ctx).GetAwaiter().GetResult();
            var ds = ctx.Items.DownstreamResponse();
            _downstreamResponse = ds;
        }
    }

    private void WhenICallTheMiddlewareWithWhiteClient(Ocelot.DownstreamRouteFinder.DownstreamRouteHolder downstreamRoute)
    {
        const string ClientId = "ocelotclient2";
        for (var i = 0; i < 10; i++)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new FakeStream();
            context.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
            context.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);
            context.Items.UpsertDownstreamRoute(downstreamRoute);
            var request = new HttpRequestMessage(new HttpMethod("GET"), _url);
            request.Headers.Add("ClientId", ClientId);
            context.Items.UpsertDownstreamRequest(new DownstreamRequest(request));
            context.Request.Headers.TryAdd("ClientId", ClientId);
            _middleware.Invoke(context).GetAwaiter().GetResult();
            var ds = context.Items.DownstreamResponse();
            _downstreamResponse = ds;
        }
    }
}

internal class FakeStream : Stream
{
    public override void Flush() { } // do nothing
    public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
    public override void SetLength(long value) => throw new NotImplementedException();
    public override void Write(byte[] buffer, int offset, int count) { } // do nothing

    public override bool CanRead { get; }
    public override bool CanSeek { get; }
    public override bool CanWrite => true;
    public override long Length { get; }
    public override long Position { get; set; }
}
