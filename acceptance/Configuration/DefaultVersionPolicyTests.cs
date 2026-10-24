using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using System.Runtime.CompilerServices;

namespace Ocelot.Acceptance.Configuration;

[Trait("Feat", "1672")] // https://github.com/ThreeMammals/Ocelot/issues/1672
public sealed class DefaultVersionPolicyTests : Steps
{
    [Fact]
    public async Task Should_return_bad_gateway_when_request_higher_receive_lower()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "2.0", VersionPolicies.RequestVersionOrHigher);
        var configuration = GivenConfiguration(route);
        GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http1);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Should_return_bad_gateway_when_request_lower_receive_higher()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "1.1", VersionPolicies.RequestVersionOrLower);
        var configuration = GivenConfiguration(route);
        GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http2);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Should_return_bad_gateway_when_request_exact_receive_different()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "1.1", VersionPolicies.RequestVersionExact);
        var configuration = GivenConfiguration(route);
        GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http2);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Should_return_ok_when_request_version_exact_receive_exact()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "2.0", VersionPolicies.RequestVersionExact);
        var configuration = GivenConfiguration(route);
        GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http2);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_return_ok_when_request_version_lower_receive_lower()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "2.0", VersionPolicies.RequestVersionOrLower);
        var configuration = GivenConfiguration(route);
        GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http1);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_return_ok_when_request_version_lower_receive_exact()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "2.0", VersionPolicies.RequestVersionOrLower);
        var configuration = GivenConfiguration(route);
        GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http2);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_return_ok_when_request_version_higher_receive_higher()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "1.1", VersionPolicies.RequestVersionOrHigher);
        var configuration = GivenConfiguration(route);
        GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http2);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_return_ok_when_request_version_higher_receive_exact()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenHttpsRoute(port, "1.1", VersionPolicies.RequestVersionOrHigher);
        var configuration = GivenConfiguration(route);
        GivenThereIsHttpsServiceRunningOn(port, HttpProtocols.Http1);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }

    private void GivenThereIsHttpsServiceRunningOn(int port, HttpProtocols protocols, [CallerMemberName] string body = "supercalifragilistic")
    {
        var url = DownstreamUrl(port, Uri.UriSchemeHttps);
        handler.GivenThereIsAServiceRunningOnWithKestrelOptions(url, string.Empty,
            options => options.ConfigureEndpointDefaults(listenOptions => { listenOptions.Protocols = protocols; }),
            context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                return context.Response.WriteAsync(body);
            });
    }

    private FileRoute GivenHttpsRoute(int port, string httpVersion, string versionPolicy)
    {
        var r = GivenRoute(port);
        r.DownstreamScheme = Uri.UriSchemeHttps; // !!!
        r.DownstreamHttpVersion = httpVersion;
        r.DownstreamHttpVersionPolicy = versionPolicy;
        r.DangerousAcceptAnyServerCertificateValidator = true;
        return r;
    }
}
