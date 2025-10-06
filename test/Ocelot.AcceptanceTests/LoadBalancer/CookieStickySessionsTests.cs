using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.Balancers;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.LoadBalancer;

[Trait("Feat", "336")] // https://github.com/ThreeMammals/Ocelot/pull/336
public sealed class CookieStickySessionsTests : Steps
{
    private readonly int[] _counters;
#if NET9_0_OR_GREATER
    private static readonly Lock SyncLock = new();
#else
    private static readonly object SyncLock = new();
#endif

    public CookieStickySessionsTests() : base()
    {
        _counters = new int[2];
    }

    [Fact]
    public void ShouldUseSameDownstreamHost_ForSingleRouteWithHighLoad()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route = GivenStickySessionsRoute([port1, port2]);
        var cookieName = route.LoadBalancerOptions.Key;
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenProductServiceIsRunning(0, port1))
            .Given(x => x.GivenProductServiceIsRunning(1, port2))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunning())
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 10, cookieName, Guid.NewGuid().ToString()))
            .Then(x => x.ThenServiceShouldHaveBeenCalledTimes(0, 10)) // RoundRobin should return first service with port1
            .Then(x => x.ThenServiceShouldHaveBeenCalledTimes(1, 0))
            .BDDfy();
    }

    [Fact]
    public void ShouldUseDifferentDownstreamHost_ForDoubleRoutesWithDifferentCookies()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenStickySessionsRoute([port1, port2]);
        var cookieName = route1.LoadBalancerOptions.Key;
        var route2 = GivenStickySessionsRoute([port2, port1], "/test", cookieName + "bestid");
        var configuration = GivenConfiguration(route1, route2);

        this.Given(x => x.GivenProductServiceIsRunning(0, port1))
            .Given(x => x.GivenProductServiceIsRunning(1, port2))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunning())
            .When(_ => WhenIGetUrlOnTheApiGatewayWithCookie("/", cookieName, "123")) // both cookies should have different values
            .When(_ => WhenIGetUrlOnTheApiGatewayWithCookie("/test", cookieName + "bestid", "123")) // stick by cookie value
            .Then(x => x.ThenServiceShouldHaveBeenCalledTimes(0, 1))
            .Then(x => x.ThenServiceShouldHaveBeenCalledTimes(1, 1))
            .BDDfy();
    }

    [Fact]
    public void ShouldUseSameDownstreamHost_ForDifferentRoutesWithSameCookie()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenStickySessionsRoute([port1, port2]);
        var cookieName = route1.LoadBalancerOptions.Key;
        var route2 = GivenStickySessionsRoute([port2, port1], "/test", cookieName);
        var configuration = GivenConfiguration(route1, route2);

        this.Given(x => x.GivenProductServiceIsRunning(0, port1))
            .Given(x => x.GivenProductServiceIsRunning(1, port2))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunning())
            .When(_ => WhenIGetUrlOnTheApiGatewayWithCookie("/", cookieName, "123"))
            .When(_ => WhenIGetUrlOnTheApiGatewayWithCookie("/test", cookieName, "123"))
            .Then(x => x.ThenServiceShouldHaveBeenCalledTimes(0, 2))
            .Then(x => x.ThenServiceShouldHaveBeenCalledTimes(1, 0))
            .BDDfy();
    }

    private FileRoute GivenStickySessionsRoute(int[] ports, string upstream = null, [CallerMemberName] string cookieName = null)
    {
        var route = GivenRoute(ports[0], upstream: upstream ?? "/");
        route.DownstreamHostAndPorts = ports.Select(Localhost).ToList();
        route.LoadBalancerOptions = new()
        {
            Type = nameof(CookieStickySessions),
            Key = cookieName, // !!!
            Expiry = 300_000,
        };
        return route;
    }

    private Task WhenIGetUrlOnTheApiGatewayMultipleTimes(string url, int times, string cookie, string value)
    {
        var tasks = new Task[times];
        for (var i = 0; i < times; i++)
        {
            tasks[i] = GetParallelTask(url, cookie, value);
        }

        return Task.WhenAll(tasks);
    }

    private async Task GetParallelTask(string url, string cookie, string value)
    {
        var response = await WhenIGetUrlOnTheApiGateway(url, cookie, value);
        var content = await response.Content.ReadAsStringAsync();
        var count = int.Parse(content);
        count.ShouldBeGreaterThan(0);
    }

    private void ThenServiceShouldHaveBeenCalledTimes(int index, int times)
    {
        _counters[index].ShouldBe(times);
    }

    private void GivenProductServiceIsRunning(int index, int port)
    {
        handler.GivenThereIsAServiceRunningOn(port, async context =>
        {
            try
            {
                string response;
                lock (SyncLock)
                {
                    _counters[index]++;
                    response = _counters[index].ToString();
                }

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync(response);
            }
            catch (Exception exception)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync(exception.StackTrace);
            }
        });
    }
}
