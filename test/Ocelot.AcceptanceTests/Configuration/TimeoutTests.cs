using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.Configuration;

[Trait("PR", "2073")] // https://github.com/ThreeMammals/Ocelot/pull/2073
public class TimeoutTests : TimeoutTestsBase
{
    [Fact]
    [Trait("Feat", "1314")] // https://github.com/ThreeMammals/Ocelot/issues/1314
    public async Task HasRouteAndGlobalTimeouts_RouteTimeoutShouldTakePrecedenceOverGlobalTimeout()
    {
        const int RouteTimeoutSeconds = 2, GlobalTimeoutSeconds = 4;
        int serviceTimeoutMs = Ms(Math.Max(RouteTimeoutSeconds, GlobalTimeoutSeconds)) + 500; // total 4.5 sec
        var port = PortFinder.GetRandomPort();
        var configuration = GivenConfiguration(port, RouteTimeoutSeconds, GlobalTimeoutSeconds); // !!!

        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, serviceTimeoutMs); // 2s -> ServiceUnavailable
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        var watcher = await WatchWhenIGetUrlOnTheApiGateway();

        ThenTimeoutIsInRange(watcher, Ms(RouteTimeoutSeconds), Ms(RouteTimeoutSeconds) + 500); // (2.0, 2.5) s
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // after 2 secs -> TimeoutException by TimeoutDelegatingHandler
        await ThenTheResponseBodyShouldBeAsync(string.Empty);
    }

    [Fact]
    [Trait("Feat", "1314")] // https://github.com/ThreeMammals/Ocelot/issues/1314
    public async Task HasGlobalTimeoutOnly_ForAllRoutesGlobalTimeoutShouldTakePrecedenceOverAbsoluteGlobalTimeout()
    {
        const int GlobalTimeoutSeconds = 2;
        int serviceTimeoutMs = Ms(GlobalTimeoutSeconds + 1); // total 3 sec
        var ports = PortFinder.GetPorts(2);
        FileRoute route1 = GivenRoute(ports[0], "/route1"), route2 = GivenRoute(ports[1], "/route2"); // without timeouts
        var configuration = GivenConfiguration(route1, route2);
        configuration.GlobalConfiguration.Timeout = GlobalTimeoutSeconds; // !!!
        GivenThereIsAServiceRunningOn(ports[0], HttpStatusCode.OK, serviceTimeoutMs); // 2s -> ServiceUnavailable
        GivenThereIsAServiceRunningOn(ports[1], HttpStatusCode.OK, serviceTimeoutMs); // 2s -> ServiceUnavailable
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        var watchers = await Task.WhenAll<Stopwatch>(
            WatchWhenIGetUrlOnTheApiGateway(route1.UpstreamPathTemplate),
            WatchWhenIGetUrlOnTheApiGateway(route2.UpstreamPathTemplate));

        int globalTimeoutMs = Ms(GlobalTimeoutSeconds);
        foreach (var watcher in watchers)
        {
            ThenTimeoutIsInRange(watcher, globalTimeoutMs, Ms(DownstreamRoute.DefaultTimeoutSeconds)); // (2.0, 90) so assert roughly
            ThenTimeoutIsInRange(watcher, globalTimeoutMs, globalTimeoutMs + 500); // (2.0, 2.5) so assert precisely
            ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // after 2 secs -> TimeoutException by TimeoutDelegatingHandler
            await ThenTheResponseBodyShouldBeAsync(string.Empty);
        }
    }

    [Fact]
    [Trait("Feat", "1869")] // https://github.com/ThreeMammals/Ocelot/issues/1869
    public async Task HasRouteTimeout_ShouldTimeoutAfterRouteTimeout()
    {
        const int RouteTimeoutSeconds = 2;
        int serviceTimeoutMs = Ms(RouteTimeoutSeconds) + 500; // total 2.5 sec
        var port = PortFinder.GetRandomPort();
        var configuration = GivenConfiguration(port, RouteTimeoutSeconds); // !!!

        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, serviceTimeoutMs); // 2.5s > 2s -> ServiceUnavailable
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        var watcher = await WatchWhenIGetUrlOnTheApiGateway();

        ThenTimeoutIsInRange(watcher, Ms(RouteTimeoutSeconds), serviceTimeoutMs);
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // after 2 secs -> TimeoutException by TimeoutDelegatingHandler
        await ThenTheResponseBodyShouldBeAsync(string.Empty);
    }
}

[Collection(nameof(SequentialTests))]
public class TimeoutSequentialTests : TimeoutTestsBase
{
    [Fact]
    [Trait("PR", "2073")] // https://github.com/ThreeMammals/Ocelot/pull/2073
    [Trait("Feat", "1869")] // https://github.com/ThreeMammals/Ocelot/issues/1869
    public async Task NoRouteTimeoutAndNoGlobalOne_ShouldTimeoutAfterCustomDefaultTimeout()
    {
        try
        {
            DownstreamRoute.DefaultTimeoutSeconds = DownstreamRoute.LowTimeout; // override original 90s with 3s
            int serviceTimeoutMs = Ms(DownstreamRoute.LowTimeout) + 500; // total 3.5 sec
            var port = PortFinder.GetRandomPort();
            var configuration = GivenConfiguration(port, routeTimeout: null, globalTimeout: null); // !!! no route timeout -> DownstreamRoute.DefaultTimeoutSeconds
            GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, serviceTimeoutMs); // 3.5s > 3s -> ServiceUnavailable
            GivenThereIsAConfiguration(configuration);
            GivenOcelotIsRunning();

            var watcher = await WatchWhenIGetUrlOnTheApiGateway();

            ThenTimeoutIsInRange(watcher, Ms(DownstreamRoute.LowTimeout), serviceTimeoutMs);
            ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // after 3 secs -> TimeoutException by TimeoutDelegatingHandler
            await ThenTheResponseBodyShouldBeAsync(string.Empty);
        }
        finally
        {
            DownstreamRoute.DefaultTimeoutSeconds = DownstreamRoute.DefTimeout;
        }
    }
}

public class TimeoutTestsBase : Steps
{
    protected static int Ms(int seconds) => 1000 * seconds;

    protected FileConfiguration GivenConfiguration(int port, int? routeTimeout = null, int? globalTimeout = null)
    {
        var route = GivenDefaultRoute(port);
        route.Timeout = routeTimeout;
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.Timeout = globalTimeout;
        return configuration;
    }

    protected void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, int timeout, [CallerMemberName] string response = nameof(TimeoutTests))
    {
        async Task MapBodyWithTimeout(HttpContext context)
        {
            await Task.Delay(timeout);
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(response);
        }
        handler.GivenThereIsAServiceRunningOn(port, MapBodyWithTimeout);
    }

    protected async Task<Stopwatch> WatchWhenIGetUrlOnTheApiGateway(string upstream = null)
    {
        var watcher = Stopwatch.StartNew();
        await WhenIGetUrlOnTheApiGateway(upstream ?? "/");
        watcher.Stop();
        return watcher;
    }

    protected static void ThenTimeoutIsInRange(Stopwatch watcher, int lowDurationMs, int highDurationMs)
    {
        var expectedLowDuration = TimeSpan.FromMilliseconds(lowDurationMs);
        var expectedHighDuration = TimeSpan.FromMilliseconds(highDurationMs);
        watcher.Elapsed.ShouldBeGreaterThan(expectedLowDuration);
        watcher.Elapsed.ShouldBeLessThan(expectedHighDuration);
    }
}
