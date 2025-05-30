using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests;

public sealed class StickySessionsTests : Steps
{
    private readonly int[] _counters;
#if NET9_0_OR_GREATER
    private static readonly Lock SyncLock = new();
#else
    private static readonly object SyncLock = new();
#endif

    public StickySessionsTests() : base()
    {
        _counters = new int[2];
    }

    [Fact]
    [Trait("Feat", "336")]
    public void ShouldUseSameDownstreamHost_ForSingleRouteWithHighLoad()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route = GivenRoute("/")
            .WithHosts(Localhost(port1), Localhost(port2));
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
    [Trait("Feat", "336")]
    public void ShouldUseDifferentDownstreamHost_ForDoubleRoutesWithDifferentCookies()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenRoute("/")
            .WithHosts(Localhost(port1), Localhost(port2));
        var cookieName = route1.LoadBalancerOptions.Key;
        var route2 = GivenRoute("/test", cookieName + "bestid")
            .WithHosts(Localhost(port2), Localhost(port1));
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
    [Trait("Feat", "336")]
    public void ShouldUseSameDownstreamHost_ForDifferentRoutesWithSameCookie()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenRoute("/")
            .WithHosts(Localhost(port1), Localhost(port2));
        var cookieName = route1.LoadBalancerOptions.Key;
        var route2 = GivenRoute("/test", cookieName)
            .WithHosts(Localhost(port2), Localhost(port1));
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

    private static FileRoute GivenRoute(string upstream, [CallerMemberName] string cookieName = null) => new()
    {
        DownstreamPathTemplate = "/",
        DownstreamScheme = Uri.UriSchemeHttp,
        UpstreamPathTemplate = upstream ?? "/",
        UpstreamHttpMethod = new() { HttpMethods.Get },
        LoadBalancerOptions = new()
        {
            Type = nameof(CookieStickySessions),
            Key = cookieName, // !!!
            Expiry = 300000,
        },
    };

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
