using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Shouldly;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;

namespace Ocelot.Testing.Steps;

public class TimeoutSteps : AcceptanceSteps
{
    public static int Ms(int seconds) => 1000 * seconds;

    public FileConfiguration GivenConfiguration(int port, int? routeTimeout = null, int? globalTimeout = null)
    {
        var route = GivenDefaultRoute(port);
        route.Timeout = routeTimeout;
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.Timeout = globalTimeout;
        return configuration;
    }

    public virtual void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, int timeout,
        [CallerMemberName] string response = nameof(TimeoutSteps))
    {
        async Task MapBodyWithTimeout(HttpContext context)
        {
            await Task.Delay(timeout);
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(response);
        }
        handler.GivenThereIsAServiceRunningOn(port, MapBodyWithTimeout);
    }

    public async Task<Stopwatch> WatchWhenIGetUrlOnTheApiGateway(string? upstream = null)
    {
        var watcher = Stopwatch.StartNew();
        await WhenIGetUrlOnTheApiGateway(upstream ?? "/");
        watcher.Stop();
        return watcher;
    }

    public static void ThenTimeoutIsInRange(Stopwatch watcher, int lowDurationMs, int highDurationMs)
    {
        var expectedLowDuration = TimeSpan.FromMilliseconds(lowDurationMs);
        var expectedHighDuration = TimeSpan.FromMilliseconds(highDurationMs);
        watcher.Elapsed.ShouldBeGreaterThan(expectedLowDuration);
        watcher.Elapsed.ShouldBeLessThan(expectedHighDuration);
    }
}
