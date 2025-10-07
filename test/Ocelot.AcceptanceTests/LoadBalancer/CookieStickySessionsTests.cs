using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.Balancers;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.LoadBalancer;

[Trait("Feat", "336")] // https://github.com/ThreeMammals/Ocelot/pull/336
public sealed class CookieStickySessionsTests : Steps
{
    private int[] _counters;
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

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2319")]
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public async Task ShouldUseGlobalOptions_ForStaticRoutes()
    {
        _counters = new int[5];
        var ports = PortFinder.GetPorts(2);
        var route1 = GivenStickySessionsRoute(ports);
        route1.LoadBalancerOptions = new(); // no load balancing -> use global opts
        var route2 = GivenStickySessionsRoute(ports.Reverse().ToArray(), "/test");
        route1.LoadBalancerOptions = new(); // no load balancing -> use global opts
        var ports2 = PortFinder.GetPorts(2);
        var route3 = GivenStickySessionsRoute(ports2, "/nextSticky", CookieName() + "-nextSticky");
        var port5 = PortFinder.GetRandomPort();
        var route4 = GivenStickySessionsRoute([port5], "/noLoadBalancing"); // this route should not be overwritten by global LB opts
        route4.LoadBalancerOptions.Type = nameof(NoLoadBalancer);

        var configuration = GivenConfiguration(route1, route2, route3, route4); // static routes come to Routes collection
        configuration.GlobalConfiguration.LoadBalancerOptions = new()
        {
            Type = nameof(CookieStickySessions),
            Key = CookieName(), // !!!
        };
        GivenProductServiceIsRunning(0, ports[0]);
        GivenProductServiceIsRunning(1, ports[1]);
        GivenProductServiceIsRunning(2, ports2[0]);
        GivenProductServiceIsRunning(3, ports2[1]);
        GivenProductServiceIsRunning(4, port5);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGatewayWithCookie("/", CookieName(), "123");
        await WhenIGetUrlOnTheApiGatewayWithCookie("/test", CookieName(), "123");
        //await WhenIGetUrlOnTheApiGatewayWithCookie("/nextSticky", CookieName() + "/nextSticky", "333");
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/nextSticky", 5, CookieName() + "-nextSticky", "333");
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/noLoadBalancing", 7, "bla-bla-cookie", "bla-bla-value");
        ThenServiceShouldHaveBeenCalledTimes(0, 2);
        ThenServiceShouldHaveBeenCalledTimes(1, 0);
        ThenServiceShouldHaveBeenCalledTimes(2, 5);
        ThenServiceShouldHaveBeenCalledTimes(3, 0);
        ThenServiceShouldHaveBeenCalledTimes(4, 7);
    }

    private static string CookieName([CallerMemberName] string cookieName = nameof(CookieStickySessionsTests)) => cookieName;

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
