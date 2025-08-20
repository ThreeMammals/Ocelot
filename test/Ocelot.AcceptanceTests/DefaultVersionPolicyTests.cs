using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

[Trait("Feat", "1672")]
public sealed class DefaultVersionPolicyTests : Steps
{
    private const string Body = "supercalifragilistic";

    public DefaultVersionPolicyTests()
    {
    }

    [Fact]
    public void Should_return_bad_gateway_when_request_higher_receive_lower()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "2.0", VersionPolicies.RequestVersionOrHigher);
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http1))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
            .BDDfy();
    }

    [Fact]
    public void Should_return_bad_gateway_when_request_lower_receive_higher()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "1.1", VersionPolicies.RequestVersionOrLower);
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http2))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
            .BDDfy();
    }

    [Fact]
    public void Should_return_bad_gateway_when_request_exact_receive_different()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "1.1", VersionPolicies.RequestVersionExact);
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http2))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
            .BDDfy();
    }

    [Fact]
    public void Should_return_ok_when_request_version_exact_receive_exact()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "2.0", VersionPolicies.RequestVersionExact);
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http2))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_return_ok_when_request_version_lower_receive_lower()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "2.0", VersionPolicies.RequestVersionOrLower);
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http1))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_return_ok_when_request_version_lower_receive_exact()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "2.0", VersionPolicies.RequestVersionOrLower);
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http2))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_return_ok_when_request_version_higher_receive_higher()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "1.1", VersionPolicies.RequestVersionOrHigher);
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http2))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_return_ok_when_request_version_higher_receive_exact()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "1.1", VersionPolicies.RequestVersionOrHigher);
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http1))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    private void GivenThereIsHttpsServiceRunningOn(int port, HttpProtocols protocols)
    {
        var url = DownstreamUrl(port, Uri.UriSchemeHttps);
        handler.GivenThereIsAServiceRunningOnWithKestrelOptions(url, string.Empty,
            options => options.ConfigureEndpointDefaults(listenOptions => { listenOptions.Protocols = protocols; }),
            context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                return context.Response.WriteAsync(Body);
            });
    }

    private static FileRoute GivenHttpsRoute(int port, string httpVersion, string versionPolicy) => new()
    {
        UpstreamPathTemplate = "/",
        UpstreamHttpMethod = [HttpMethods.Get],
        DownstreamPathTemplate = "/",
        DownstreamHostAndPorts = new() { new("localhost", port) },
        DownstreamScheme = Uri.UriSchemeHttps, // !!!
        DownstreamHttpVersion = httpVersion,
        DownstreamHttpVersionPolicy = versionPolicy,
        DangerousAcceptAnyServerCertificateValidator = true,
    };
}
