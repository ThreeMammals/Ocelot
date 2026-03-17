using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester;

namespace Ocelot.AcceptanceTests.Requester;

public sealed class MessageInvokerPoolTests : RequesterSteps
{
    #region TODO Redevelop to minimize the code
    [Fact]
    [Trait("PR", "1824")] // https://github.com/ThreeMammals/Ocelot/pull/1824
    public async Task Should_reuse_cookies_from_container()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(new())
            .WithHttpHandlerOptions(new() { UseCookieContainer = true, UseProxy = true })
            .WithLoadBalancerKey(string.Empty)
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().WithOriginalValue(string.Empty).Build())

            // The test should pass without timeout definition -> implicit default timeout
            //.WithTimeout(DownstreamRoute.DefaultTimeoutSeconds)
            .Build();

        //using ServiceHandler handler = new();
        var port = PortFinder.GetRandomPort();
        GivenADownstreamService(port); // sometimes it fails because of port binding

        GivenTheFactoryReturns(new());
        GivenAMessageInvokerPool();
        GivenARequest(route, port);

        // Act, Assert
        var toUrl = DownstreamUrl(port);
        await WhenICallTheClient(toUrl);
        _response.Headers.TryGetValues("Set-Cookie", out _).ShouldBeTrue();

        // Act, Assert
        await WhenICallTheClient(toUrl);
        _response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
    private Mock<IDelegatingHandlerFactory> _handlerFactory;
    private HttpResponseMessage _response;
    private MessageInvokerPool _pool;
    private readonly DefaultHttpContext _context = new();
    private readonly Mock<IOcelotLogger> _ocelotLogger = new();
    private readonly Mock<IOcelotLoggerFactory> _ocelotLoggerFactory = new();
    private async Task WhenICallTheClient(string url)
    {
        var messageInvoker = _pool.Get(_context.Items.DownstreamRoute());
        _response = await messageInvoker
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, url), CancellationToken.None);
    }
    private void GivenAMessageInvokerPool() =>
        _pool = new MessageInvokerPool(_handlerFactory.Object, _ocelotLoggerFactory.Object);
    private void GivenTheFactoryReturns(List<DelegatingHandler> handlers)
    {
        _handlerFactory = new Mock<IDelegatingHandlerFactory>();
        _handlerFactory.Setup(x => x.Get(It.IsAny<DownstreamRoute>()))
            .Returns(handlers);
    }
    private void GivenARequest(DownstreamRoute downstream, int port)
        => GivenARequestWithAUrlAndMethod(downstream, port, HttpMethod.Get);
    private void GivenARequestWithAUrlAndMethod(DownstreamRoute downstream, int port, HttpMethod method)
    {
        var url = DownstreamUrl(port);
        _context.Items.UpsertDownstreamRoute(downstream);
        _context.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage
        { RequestUri = new Uri(url), Method = method }));
    }

    private void GivenADownstreamService(int port)
    {
        var count = 0;
        handler.GivenThereIsAServiceRunningOn(port, context =>
        {
            if (count == 0)
            {
                context.Response.Cookies.Append("test", "0");
                context.Response.StatusCode = 200;
                count++;
                return Task.CompletedTask;
            }

            if (count == 1)
            {
                if (context.Request.Cookies.TryGetValue("test", out var cookieValue) ||
                    context.Request.Headers.TryGetValue("Set-Cookie", out var headerValue))
                {
                    context.Response.StatusCode = 200;
                    return Task.CompletedTask;
                }

                context.Response.StatusCode = 500;
            }

            return Task.CompletedTask;
        });
    }
    #endregion

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
        GivenOcelotIsRunning(WithRequesterTesting);

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
        await GivenOcelotIsRunningAsync(WithRequesterTesting);

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
        var pool = OcelotServices.GetService<IMessageInvokerPool>() as TestMessageInvokerPool;
        var tracer = OcelotServices.GetService<IOcelotTracer>() as TestTracer;
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
}
