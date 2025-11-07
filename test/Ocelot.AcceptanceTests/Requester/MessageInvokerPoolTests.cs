using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Requester;

namespace Ocelot.AcceptanceTests.Requester;

public sealed class MessageInvokerPoolTests : Steps
{
    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2320")]
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
    public async Task ShouldApplyGlobalHttpHandlerOptions_ForStaticRoutes()
    {
        var ports = PortFinder.GetPorts(3);
        var route1 = GivenRoute(ports[0], "/route1", null); // no opts -> use global opts
        var route2 = GivenRoute(ports[1], "/route2", GivenOptions(99, 99, useTracing: true));
        var route3 = GivenRoute(ports[2], "/noTracing", GivenOptions());
        var configuration = GivenConfiguration(route1, route2, route3); // static routes come to Routes collection
        var globalOptions = configuration.GlobalConfiguration.HttpHandlerOptions = new(GivenOptions(100, 100, useTracing: false));

        GivenThereIsAServiceRunningOnPath(ports[0], "/route1");
        GivenThereIsAServiceRunningOnPath(ports[1], "/route2");
        GivenThereIsAServiceRunningOnPath(ports[2], "/noTracing");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithTesting);

        await WhenIGetUrlOnTheApiGateway("/route1");
        await WhenIGetUrlOnTheApiGateway("/route2");
        await WhenIGetUrlOnTheApiGateway("/noTracing");

        ThenTheResponseBody();
        ThenRouteHttpHandlerOptionsAre(route1, globalOptions.MaxConnectionsPerServer.Value, globalOptions.PooledConnectionLifetimeSeconds.Value, globalOptions.UseTracing.Value);
        ThenRouteHttpHandlerOptionsAre(route2, 99, 99, true);
        ThenRouteHttpHandlerOptionsAre(route3, 100, 100, false);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2320")]
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
    public async Task ShouldApplyGlobalGroupHttpHandlerOptions_ForStaticRoutes_WhenRouteOptsHasAKey()
    {
        // 1st route
        var ports = PortFinder.GetPorts(3);
        var route1 = GivenRoute(ports[0], "/route1", null);
        route1.Key = null; // 1st route is not in the global group

        // 2nd route
        var route2 = GivenRoute(ports[1], "/route2", null); // 2nd route opts will be applied from global ones
        route2.Key = "R2"; // 2nd route is in the group

        // 3rd route
        var route3 = GivenRoute(ports[2], "/noTracing", GivenOptions(88, 88, useTracing: false));

        var configuration = GivenConfiguration(route1, route2, route3);
        var globalOptions = configuration.GlobalConfiguration.HttpHandlerOptions = new(GivenOptions(100, 100, useTracing: true))
        {
            RouteKeys = ["R2"],
        };

        GivenThereIsAServiceRunningOnPath(ports[0], "/route1");
        GivenThereIsAServiceRunningOnPath(ports[1], "/route2");
        GivenThereIsAServiceRunningOnPath(ports[2], "/noTracing");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithTesting);

        await WhenIGetUrlOnTheApiGateway("/route1");
        await WhenIGetUrlOnTheApiGateway("/route2");
        await WhenIGetUrlOnTheApiGateway("/noTracing");

        ThenTheResponseBody();
        ThenRouteHttpHandlerOptionsAre(route1, int.MaxValue, HttpHandlerOptions.DefaultPooledConnectionLifetimeSeconds, false);
        ThenRouteHttpHandlerOptionsAre(route2, globalOptions.MaxConnectionsPerServer.Value, globalOptions.PooledConnectionLifetimeSeconds.Value, globalOptions.UseTracing.Value);
        ThenRouteHttpHandlerOptionsAre(route3, 88, 88, false);
    }

    private void ThenRouteHttpHandlerOptionsAre(FileRoute route, int maxConnections, int seconds, bool useTracing)
    {
        var pool = ocelotServer.Services.GetService<IMessageInvokerPool>() as TestMessageInvokerPool;
        var tracer = ocelotServer.Services.GetService<IOcelotTracer>() as TestTracer;
        var kv = pool.ShouldNotBeNull()
            .CreatedHandlers.Single(x => x.Key.UpstreamPathTemplate.OriginalValue == route.UpstreamPathTemplate);
        var downstream = kv.Key;
        var httpHandler = kv.Value;
        httpHandler.MaxConnectionsPerServer.ShouldBe(maxConnections);
        httpHandler.PooledConnectionLifetime.TotalSeconds.ShouldBe(seconds);
        downstream.HttpHandlerOptions.UseTracing.ShouldBe(useTracing);
        var request = tracer.Requests.Keys.SingleOrDefault(k => k.RequestUri.AbsolutePath == route.UpstreamPathTemplate);
        (request != null).ShouldBe(useTracing);
    }

    private static FileHttpHandlerOptions GivenOptions(int maxConnections = 100, int pooledConnectionSeconds = 100,
        bool useCookieContainer = false, bool useProxy = false, bool useTracing = false) => new()
    {
        MaxConnectionsPerServer = maxConnections,
        PooledConnectionLifetimeSeconds = pooledConnectionSeconds,
        UseCookieContainer = useCookieContainer,
        UseProxy = useProxy,
        UseTracing = useTracing,
    };

    private FileRoute GivenRoute(int port, string path = null, FileHttpHandlerOptions options = null)
    {
        var r = GivenRoute(port, path, path);
        r.HttpHandlerOptions = options;
        return r;
    }

    private static void WithTesting(IServiceCollection services) => services
        .AddOcelot().Services
        .AddSingleton<IOcelotTracer, TestTracer>()
        .RemoveAll<IMessageInvokerPool>()
        .AddSingleton<IMessageInvokerPool, TestMessageInvokerPool>();
}

public class TestMessageInvokerPool : MessageInvokerPool, IMessageInvokerPool
{
    public TestMessageInvokerPool(IDelegatingHandlerFactory handlerFactory, IOcelotLoggerFactory loggerFactory)
        : base(handlerFactory, loggerFactory) { }

    public readonly Dictionary<DownstreamRoute, SocketsHttpHandler> CreatedHandlers = new();
    protected override SocketsHttpHandler CreateHandler(DownstreamRoute route)
    {
        var handler = base.CreateHandler(route);
        CreatedHandlers[route] = handler;
        return handler;
    }
}

public class TestTracer : IOcelotTracer
{
    public readonly List<string> Events = new();
    public readonly Dictionary<HttpRequestMessage, HttpResponseMessage> Requests = new();

    public void Event(HttpContext httpContext, string @event)
        => Events.Add(@event);

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Action<string> addTraceIdToRepo, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> baseSendAsync, CancellationToken cancellationToken)
    {
        addTraceIdToRepo?.Invoke("12345");
        var response = await baseSendAsync.Invoke(request, cancellationToken).ConfigureAwait(false);
        Requests[request] = response;
        return response;
    }
}
