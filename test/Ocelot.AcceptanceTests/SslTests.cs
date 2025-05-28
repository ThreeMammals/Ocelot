using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public sealed class SslTests : Steps
{
    public SslTests()
    {
    }

    [Fact]
    public void Should_dangerous_accept_any_server_certificate_validator()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenSslRoute(port, true);
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
    public void Should_not_dangerous_accept_any_server_certificate_validator()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenSslRoute(port, false);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
            .BDDfy();
    }

    private static FileRoute GivenSslRoute(int port, bool validatorEnabled)
    {
        var route = GivenDefaultRoute(port);
        route.DownstreamScheme = Uri.UriSchemeHttps;
        route.DangerousAcceptAnyServerCertificateValidator = validatorEnabled;
        return route;
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        handler.GivenThereIsAHttpsServiceRunningOn(DownstreamUrl(port), basePath, "mycert.pfx", "password", port, async context =>
        {
            var downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;
            bool oK = downstreamPath == basePath;
            context.Response.StatusCode = oK ? (int)statusCode : (int)HttpStatusCode.NotFound;
            await context.Response.WriteAsync(oK ? responseBody : "downstream path didn't match base path");
        });
    }
}
