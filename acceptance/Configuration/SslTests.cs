using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.Acceptance.Configuration;

[Trait("Feat", "309")] // https://github.com/ThreeMammals/Ocelot/issues/309
public sealed class SslTests : Steps
{
    [Fact]
    [Trait("PR", "325")] // https://github.com/ThreeMammals/Ocelot/pull/325
    public async Task Should_dangerous_accept_any_server_certificate_validator()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenSslRoute(port, true);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Hello from Laura");
    }

    [Fact]
    [Trait("PR", "325")] // https://github.com/ThreeMammals/Ocelot/pull/325
    public async Task Should_not_dangerous_accept_any_server_certificate_validator()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenSslRoute(port, false);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway);
    }

    private FileRoute GivenSslRoute(int port, bool validatorEnabled)
    {
        var route = GivenDefaultRoute(port);
        route.DownstreamScheme = Uri.UriSchemeHttps;
        route.DangerousAcceptAnyServerCertificateValidator = validatorEnabled;
        return route;
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        handler.GivenThereIsAHttpsServiceRunningOn(DownstreamUrl(port), basePath, "mycert2.pfx", "password", port, async context =>
        {
            var downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;
            bool oK = downstreamPath == basePath;
            context.Response.StatusCode = oK ? (int)statusCode : (int)HttpStatusCode.NotFound;
            await context.Response.WriteAsync(oK ? responseBody : "downstream path didn't match base path");
        });
    }
}
