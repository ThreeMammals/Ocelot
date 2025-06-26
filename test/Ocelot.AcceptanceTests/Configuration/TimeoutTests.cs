using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.Configuration;

public class TimeoutTests : TimeoutTestsBase
{
    [Fact]
    [Trait("PR", "2073")] // https://github.com/ThreeMammals/Ocelot/pull/2073
    [Trait("Feat", "1869")] // https://github.com/ThreeMammals/Ocelot/issues/1869
    public async Task HasRouteTimeout_ShouldTimeoutAfterRouteTimeout()
    {
        const int RouteTimeoutSeconds = 2;
        int serviceTimeoutMs = (1000 * RouteTimeoutSeconds) + 500; // total 2.5 sec
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        route.Timeout = RouteTimeoutSeconds; // !!!

        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, serviceTimeoutMs); // 2.5s > 2s -> ServiceUnavailable
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        var watcher = await WatchWhenIGetUrlOnTheApiGateway();

        ThenTimeoutIsInRange(watcher, RouteTimeoutSeconds * 1000, serviceTimeoutMs);
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
    public async Task NoRouteTimeout_ShouldTimeoutAfterCustomDefaultTimeout()
    {
        try
        {
            DownstreamRoute.DefaultTimeoutSeconds = DownstreamRoute.LowTimeout; // override original 90s with 3s
            int serviceTimeoutMs = (1000 * DownstreamRoute.LowTimeout) + 500; // total 3.5 sec
            var port = PortFinder.GetRandomPort();
            var route = GivenRoute(port); // !!! no route timeout -> DownstreamRoute.DefaultTimeoutSeconds
            var configuration = GivenConfiguration(route);
            GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, serviceTimeoutMs); // 3.5s > 3s -> ServiceUnavailable
            GivenThereIsAConfiguration(configuration);
            GivenOcelotIsRunning();

            var watcher = await WatchWhenIGetUrlOnTheApiGateway();

            ThenTimeoutIsInRange(watcher, DownstreamRoute.LowTimeout * 1000, serviceTimeoutMs);
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

    protected async Task<Stopwatch> WatchWhenIGetUrlOnTheApiGateway()
    {
        var watcher = Stopwatch.StartNew();
        await WhenIGetUrlOnTheApiGateway("/");
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
