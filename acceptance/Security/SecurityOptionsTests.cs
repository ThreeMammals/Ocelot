using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides; // !!!
using Ocelot.Configuration.File;
using Ocelot.Middleware;

namespace Ocelot.AcceptanceTests.Security;

public sealed class SecurityOptionsTests: Steps
{
    [Fact]
    [Trait("Feat", "2170")] // https://github.com/ThreeMammals/Ocelot/pull/2170
    public void Should_call_with_allowed_ip_in_global_config()
    {
        var ip = "192.168.1.35";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/worldPath", "/myPath");
        GivenRouteForwardingClientIpAddressViaHeader(route, ForwardedHeadersDefaults.XForwardedForHeaderName);
        var configuration = GivenGlobalConfiguration(route, "192.168.1.30-50", "192.168.1.1-100");
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, route.DownstreamPathTemplate, EchoHeaders))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithUseForwardedHeaders))
            .And(x => GivenIAddAHeader(ForwardedHeadersDefaults.XForwardedForHeaderName, ip))
            .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Fabrizio"))
            .And(x => ThenTheResponseHeaderIs(ForwardedHeadersDefaults.XForwardedForHeaderName, ip))
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "2170")] // https://github.com/ThreeMammals/Ocelot/pull/2170
    public async Task Should_block_call_with_blocked_ip_in_global_config()
    {
        var ip = "192.168.1.55";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/worldPath", "/myPath");
        GivenRouteForwardingClientIpAddressViaHeader(route, ForwardedHeadersDefaults.XForwardedForHeaderName);
        var configuration = GivenGlobalConfiguration(route, "192.168.1.30-50", "192.168.1.1-100");
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, route.DownstreamPathTemplate, EchoHeaders))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithUseForwardedHeaders))
            .And(x => GivenIAddAHeader(ForwardedHeadersDefaults.XForwardedForHeaderName, ip))
            .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
            .And(x => ThenTheResponseBodyShouldBeEmpty())
            .And(x => ThenTheResponseHeaderExists(ForwardedHeadersDefaults.XForwardedForHeaderName, false))
        .BDDfy();
    }

    [Fact]
    public void Should_call_with_allowed_ip_in_route_config()
    {
        var ip = "192.168.1.1";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/worldPath", "/myPath");
        GivenRouteForwardingClientIpAddressViaHeader(route, ForwardedHeadersDefaults.XForwardedForHeaderName);
        route.SecurityOptions = new()
        {
            IPAllowedList = [ip],
        };
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, route.DownstreamPathTemplate, EchoHeaders))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithUseForwardedHeaders))
            .And(x => GivenIAddAHeader(ForwardedHeadersDefaults.XForwardedForHeaderName, ip))
            .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Fabrizio"))
            .And(x => ThenTheResponseHeaderIs(ForwardedHeadersDefaults.XForwardedForHeaderName, ip))
        .BDDfy();
    }

    [Fact]
    public void Should_block_call_with_blocked_ip_in_route_config()
    {
        var ip = "192.168.1.1";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/worldPath", "/myPath");
        GivenRouteForwardingClientIpAddressViaHeader(route, ForwardedHeadersDefaults.XForwardedForHeaderName);
        route.SecurityOptions = new()
        {
            IPBlockedList = [ip],
        };
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, route.DownstreamPathTemplate, EchoHeaders))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithUseForwardedHeaders))
            .And(x => GivenIAddAHeader(ForwardedHeadersDefaults.XForwardedForHeaderName, ip))
            .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
            .And(x => ThenTheResponseBodyShouldBeEmpty())
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "2170")] // https://github.com/ThreeMammals/Ocelot/pull/2170
    public void Should_call_with_allowed_ip_in_route_config_and_blocked_ip_in_global_config()
    {
        var ip = "192.168.1.55";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/worldPath", "/myPath");
        GivenRouteForwardingClientIpAddressViaHeader(route, ForwardedHeadersDefaults.XForwardedForHeaderName);
        route.SecurityOptions = new()
        {
            IPAllowedList = [ip],
        };
        var configuration = GivenGlobalConfiguration(route, "192.168.1.30-50", "192.168.1.1-100");
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, route.DownstreamPathTemplate, EchoHeaders))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithUseForwardedHeaders))
            .And(x => GivenIAddAHeader(ForwardedHeadersDefaults.XForwardedForHeaderName, ip))
            .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Fabrizio"))
            .And(x => ThenTheResponseHeaderIs(ForwardedHeadersDefaults.XForwardedForHeaderName, ip))
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "2170")] // https://github.com/ThreeMammals/Ocelot/pull/2170
    public void Should_block_call_with_blocked_ip_in_route_config_and_allowed_ip_in_global_config()
    {
        var ip = "192.168.1.35";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/worldPath", "/myPath");
        GivenRouteForwardingClientIpAddressViaHeader(route, ForwardedHeadersDefaults.XForwardedForHeaderName);
        route.SecurityOptions = new()
        {
            IPBlockedList = [ip],
        };
        var configuration = GivenGlobalConfiguration(route, "192.168.1.30-50", "192.168.1.1-100");
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, route.DownstreamPathTemplate, EchoHeaders))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithUseForwardedHeaders))
            .And(x => GivenIAddAHeader(ForwardedHeadersDefaults.XForwardedForHeaderName, ip))
            .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
            .And(x => ThenTheResponseBodyShouldBeEmpty())
        .BDDfy();
    }

    private static void GivenRouteForwardingClientIpAddressViaHeader(FileRoute r, string header)
        => r.UpstreamHeaderTransform.Add(header, "{RemoteIpAddress}"); // forward the header to downstream

    private static Task EchoHeaders(HttpContext context)
    {
        // context.Connection.RemoteIpAddress = ipAddess;
        foreach (var h in context.Request.Headers)
            context.Response.Headers.Add(h);
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        return context.Response.WriteAsync("Hello from Fabrizio");
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

    private static void WithUseForwardedHeaders(IApplicationBuilder app)
    {
        // Tell the middleware to look for the X-Forwarded-For header
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
        app.UseOcelot().GetAwaiter().GetResult();
    }
}
