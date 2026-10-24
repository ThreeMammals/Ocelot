using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Testing.Steps;
using System.Diagnostics;

namespace Ocelot.Acceptance.Configuration;

[Trait("PR", "2073")] // https://github.com/ThreeMammals/Ocelot/pull/2073
public class TimeoutTests : TimeoutSteps
{
    [Fact]
    [Trait("Feat", "1314")] // https://github.com/ThreeMammals/Ocelot/issues/1314
    public async Task HasRouteAndGlobalTimeouts_RouteTimeoutShouldTakePrecedenceOverGlobalTimeout()
    {
        const int RouteTimeoutSeconds = 2, GlobalTimeoutSeconds = 4;
        int serviceTimeoutMs = Ms(Math.Max(RouteTimeoutSeconds, GlobalTimeoutSeconds)) + 500; // total 4.5 sec
        var port = PortFinder.GetRandomPort();
        var configuration = GivenConfiguration(port, RouteTimeoutSeconds, GlobalTimeoutSeconds); // !!!
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, serviceTimeoutMs); // 2s -> GatewayTimeout
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenImWatchingWhenIGetUrlOnTheApiGateway();
        ThenTimeoutIsInRange(_watcher, Ms(RouteTimeoutSeconds), Ms(RouteTimeoutSeconds) + 500); // (2.0, 2.5) s
        ThenTheStatusCodeShouldBe(HttpStatusCode.GatewayTimeout); // after 2 secs -> TimeoutException by TimeoutDelegatingHandler
        await ThenTheResponseBodyShouldBeEmpty();
    }

    [Fact]
    [Trait("Feat", "1314")] // https://github.com/ThreeMammals/Ocelot/issues/1314
    public async Task HasGlobalTimeoutOnly_ForAllRoutesGlobalTimeoutShouldTakePrecedenceOverAbsoluteGlobalTimeout()
    {
        const int GlobalTimeoutSeconds = 2;
        int serviceTimeoutMs = Ms(GlobalTimeoutSeconds + 1); // total 3 sec
        var ports = PortFinder.GetPorts(2);
        FileRoute route1 = GivenRoute(ports[0], "/route1"),
            route2 = GivenRoute(ports[1], "/route2"); // without timeouts
        var configuration = GivenConfiguration(route1, route2);
        configuration.GlobalConfiguration.Timeout = GlobalTimeoutSeconds; // !!!

        int globalTimeoutMs = Ms(GlobalTimeoutSeconds);
        async void ThenAssertAllWatchersHavingTheTimeoutIsInRange(Stopwatch watcher)
        {
            ThenTimeoutIsInRange(watcher, globalTimeoutMs, Ms(DownstreamRoute.DefaultTimeoutSeconds)); // (2.0, 90) so assert roughly
            ThenTimeoutIsInRange(watcher, globalTimeoutMs, globalTimeoutMs + 500); // (2.0, 2.5) so assert precisely
            ThenTheStatusCodeShouldBe(HttpStatusCode.GatewayTimeout); // after 2 secs -> TimeoutException by TimeoutDelegatingHandler
            await ThenTheResponseBodyShouldBeAsync(string.Empty);
        }
        GivenThereIsAServiceRunningOn(ports[0], HttpStatusCode.OK, serviceTimeoutMs); // 2s -> GatewayTimeout
        GivenThereIsAServiceRunningOn(ports[1], HttpStatusCode.OK, serviceTimeoutMs); // 2s -> GatewayTimeout
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenImWatchingBothRoutesWhenIGetUrlOnTheApiGateway(route1, route2);
        watchers.ToList().ForEach(ThenAssertAllWatchersHavingTheTimeoutIsInRange);
    }
    private Stopwatch[] watchers = default;
    private async Task WhenImWatchingBothRoutesWhenIGetUrlOnTheApiGateway(FileRoute route1, FileRoute route2)
        => watchers = await Task.WhenAll<Stopwatch>(
            WatchWhenIGetUrlOnTheApiGateway(route1.UpstreamPathTemplate),
            WatchWhenIGetUrlOnTheApiGateway(route2.UpstreamPathTemplate));

    [Fact]
    [Trait("Bug", "1687")] // https://github.com/ThreeMammals/Ocelot/issues/1687
    [Trait("Feat", "1869")] // https://github.com/ThreeMammals/Ocelot/issues/1869
    public async Task HasRouteTimeout_ShouldTimeoutAfterRouteTimeout()
    {
        const int RouteTimeoutSeconds = 2;
        int serviceTimeoutMs = Ms(RouteTimeoutSeconds + 1); // total 3 sec
        var port = PortFinder.GetRandomPort();
        var configuration = GivenConfiguration(port, RouteTimeoutSeconds); // !!!
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, serviceTimeoutMs); // 3s > 2s -> GatewayTimeout
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenImWatchingWhenIGetUrlOnTheApiGateway();
        ThenTimeoutIsInRange(_watcher, Ms(RouteTimeoutSeconds), serviceTimeoutMs);
        ThenTheStatusCodeShouldBe(HttpStatusCode.GatewayTimeout); // after 2 secs -> TimeoutException by TimeoutDelegatingHandler
        await ThenTheResponseBodyShouldBeEmpty();
    }

    [Collection(nameof(SequentialTests))]
    public class Sequential : TimeoutSteps
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
                GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, serviceTimeoutMs); // 3.5s > 3s -> GatewayTimeout
                GivenThereIsAConfiguration(configuration);
                GivenOcelotIsRunning();
                await WhenImWatchingWhenIGetUrlOnTheApiGateway();
                ThenTimeoutIsInRange(_watcher, Ms(DownstreamRoute.LowTimeout), serviceTimeoutMs);
                ThenTheStatusCodeShouldBe(HttpStatusCode.GatewayTimeout); // after 3 secs -> TimeoutException by TimeoutDelegatingHandler
                await ThenTheResponseBodyShouldBeEmpty();
            }
            finally
            {
                DownstreamRoute.DefaultTimeoutSeconds = DownstreamRoute.DefTimeout;
            }
        }
        protected Stopwatch _watcher;
        protected async Task WhenImWatchingWhenIGetUrlOnTheApiGateway()
            => _watcher = await WatchWhenIGetUrlOnTheApiGateway();
    }

    protected Stopwatch _watcher;
    protected async Task WhenImWatchingWhenIGetUrlOnTheApiGateway()
        => _watcher = await WatchWhenIGetUrlOnTheApiGateway();
}
