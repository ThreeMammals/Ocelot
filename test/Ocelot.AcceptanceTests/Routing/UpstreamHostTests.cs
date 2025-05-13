using Microsoft.AspNetCore.Http;

namespace Ocelot.AcceptanceTests.Routing;

/// <summary>
/// Feature: <see href="https://ocelot.readthedocs.io/en/latest/features/routing.html#upstream-host">Upstream Host</see>,
/// with docs: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/routing.rst#upstream-host-4">Upstream Host</see>.
/// </summary>
public sealed class UpstreamHostTests : Steps
{
    public UpstreamHostTests()
    {
    }

    [Fact]
    public void Should_return_response_200_with_simple_url_and_hosts_match()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port).WithUpstreamHost("localhost");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_simple_url_and_hosts_match_multiple_re_routes()
    {
        var port = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port).WithUpstreamHost("localhost");
        var route2 = GivenDefaultRoute(port2).WithUpstreamHost("DONTMATCH");
        var configuration = GivenConfiguration(route, route2);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_simple_url_and_hosts_match_multiple_re_routes_reversed()
    {
        var port = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port).WithUpstreamHost("DONTMATCH");
        var route2 = GivenDefaultRoute(port2).WithUpstreamHost("localhost");
        var configuration = GivenConfiguration(route, route2);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port2, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_simple_url_and_hosts_match_multiple_re_routes_reversed_with_no_host_first()
    {
        var port = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port).WithUpstreamHost(null);
        var route2 = GivenDefaultRoute(port2).WithUpstreamHost("localhost");
        var configuration = GivenConfiguration(route, route2);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port2, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_404_with_simple_url_and_hosts_dont_match()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port).WithUpstreamHost("127.0.0.20:5000");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, context =>
        {
            var downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;
            bool oK = downstreamPath == basePath;
            context.Response.StatusCode = oK ? (int)statusCode : (int)HttpStatusCode.NotFound;
            return context.Response.WriteAsync(oK ? responseBody : "downstream path didn't match base path");
        });
    }
}
