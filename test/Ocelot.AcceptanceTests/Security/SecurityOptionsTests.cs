using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests.Security;

public sealed class SecurityOptionsTests: Steps
{
    [BddfyFact]
    [Trait("Feat", "2170")] // https://github.com/ThreeMammals/Ocelot/pull/2170
    public void Should_call_with_allowed_ip_in_global_config()
    {
        var port = PortFinder.GetRandomPort();
        var ip = Dns.GetHostAddresses("192.168.1.35")[0];
        var route = GivenRoute(port, "/worldPath", "/myPath");
        var configuration = GivenGlobalConfiguration(route, "192.168.1.30-50", "192.168.1.1-100");
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, ip))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Feat", "2170")] // https://github.com/ThreeMammals/Ocelot/pull/2170
    public void Should_block_call_with_blocked_ip_in_global_config()
    {
        var port = PortFinder.GetRandomPort();
        var ip = Dns.GetHostAddresses("192.168.1.55")[0];
        var route = GivenRoute(port, "/worldPath", "/myPath");
        var configuration = GivenGlobalConfiguration(route, "192.168.1.30-50", "192.168.1.1-100");
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, ip))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
        .BDDfy();
    }

    [BddfyFact]
    public void Should_call_with_allowed_ip_in_route_config()
    {
        var port = PortFinder.GetRandomPort();
        var ip = Dns.GetHostAddresses("192.168.1.1")[0];
        var route = GivenRoute(port, "/worldPath", "/myPath");
        route.SecurityOptions = new()
        {
            IPAllowedList = [ "192.168.1.1" ],
        };
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, ip))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
        .BDDfy();
    }

    [BddfyFact]
    public void Should_block_call_with_blocked_ip_in_route_config()
    {
        var port = PortFinder.GetRandomPort();
        var ip = Dns.GetHostAddresses("192.168.1.1")[0];
        var route = GivenRoute(port, "/worldPath", "/myPath");
        route.SecurityOptions = new()
        {
            IPBlockedList = [ "192.168.1.1" ],
        };
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, ip))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Feat", "2170")] // https://github.com/ThreeMammals/Ocelot/pull/2170
    public void Should_call_with_allowed_ip_in_route_config_and_blocked_ip_in_global_config()
    {
        var port = PortFinder.GetRandomPort();
        var ip = Dns.GetHostAddresses("192.168.1.55")[0];
        var route = GivenRoute(port, "/worldPath", "/myPath");
        route.SecurityOptions = new()
        {
            IPAllowedList = [ "192.168.1.55" ],
        };
        var configuration = GivenGlobalConfiguration(route, "192.168.1.30-50", "192.168.1.1-100");
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, ip))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .Then(x => ThenTheResponseBodyShouldBe("Hello from Fabrizio"))
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Feat", "2170")] // https://github.com/ThreeMammals/Ocelot/pull/2170
    public void Should_block_call_with_blocked_ip_in_route_config_and_allowed_ip_in_global_config()
    {
        var port = PortFinder.GetRandomPort();
        var ip = Dns.GetHostAddresses("192.168.1.35")[0];
        var route = GivenRoute(port, "/worldPath", "/myPath");
        route.SecurityOptions = new()
        {
            IPBlockedList = [ "192.168.1.35" ],
        };
        var configuration = GivenGlobalConfiguration(route, "192.168.1.30-50", "192.168.1.1-100");
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, ip))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
        .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(int port, IPAddress ipAddess)
    {
        handler.GivenThereIsAServiceRunningOn(port, context =>
        {
            context.Connection.RemoteIpAddress = ipAddess;
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            return context.Response.WriteAsync("Hello from Fabrizio");
        });
    }

    private FileConfiguration GivenGlobalConfiguration(FileRoute route, string allowed, string blocked, bool exclude = true)
    {
        var config = GivenConfiguration(route);
        config.GlobalConfiguration.SecurityOptions = new()
        {
            IPAllowedList = new() { allowed },
            IPBlockedList = new() { blocked },
            ExcludeAllowedFromBlocked = exclude,
        };
        return config;
    }
}
