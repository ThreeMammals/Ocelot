using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Ocelot.Configuration.File;
using System.Security.Authentication;

namespace Ocelot.AcceptanceTests.Configuration;

[Trait("PR", "1127")]
[Trait("Feat", "1124")]
public sealed class DownstreamHttpVersionTests : Steps
{
    [Fact]
    public void Should_return_response_200_when_using_http_one()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, Uri.UriSchemeHttp, HttpVersion.Version10);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpProtocols.Http1))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_when_using_http_one_point_one()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, Uri.UriSchemeHttp, HttpVersion.Version11);
        route.DangerousAcceptAnyServerCertificateValidator = true;
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpProtocols.Http1))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_when_using_http_two_point_zero()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, Uri.UriSchemeHttps, HttpVersion.Version20);
        route.DangerousAcceptAnyServerCertificateValidator = true;
        var configuration = GivenConfiguration(route);

        const string expected = "here is some content";
        var httpContent = new StringContent(expected);

        this.Given(x => x.GivenThereIsAServiceUsingHttpsRunningOn(port, "/", HttpProtocols.Http2))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/", httpContent))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe(expected))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_502_when_using_http_one_to_talk_to_server_running_http_two()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, Uri.UriSchemeHttps, HttpVersion.Version11);
        route.DangerousAcceptAnyServerCertificateValidator = true;
        var configuration = GivenConfiguration(route);

        const string expected = "here is some content";
        var httpContent = new StringContent(expected);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpProtocols.Http2))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/", httpContent))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
            .BDDfy();
    }

    //TODO: does this test make any sense?
    [Fact]
    public void Should_return_response_200_when_using_http_two_to_talk_to_server_running_http_one_point_one()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, Uri.UriSchemeHttp, HttpVersion.Version11);
        route.DangerousAcceptAnyServerCertificateValidator = true;
        var configuration = GivenConfiguration(route);

        const string expected = "here is some content";
        var httpContent = new StringContent(expected);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpProtocols.Http1))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/", httpContent))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe(expected))
            .BDDfy();
    }

    private FileRoute GivenRoute(int port, string scheme, Version httpVersion) => new()
    {
        DownstreamPathTemplate = "/{url}",
        DownstreamScheme = scheme ?? Uri.UriSchemeHttp,
        UpstreamPathTemplate = "/{url}",
        UpstreamHttpMethod = [HttpMethods.Get],
        DownstreamHostAndPorts = [Localhost(port)],
        DownstreamHttpMethod = HttpMethods.Get,
        DownstreamHttpVersion = httpVersion.ToString(),
    };

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpProtocols protocols)
    {
        void options(KestrelServerOptions serverOptions)
        {
            serverOptions.Listen(IPAddress.Loopback, port, listenOptions =>
            {
                listenOptions.Protocols = protocols;
            });
        }

        handler.GivenThereIsAServiceRunningOnWithKestrelOptions(DownstreamUrl(port), basePath, options, async context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            await context.Response.WriteAsync(body);
        });
    }

    private void GivenThereIsAServiceUsingHttpsRunningOn(int port, string basePath, HttpProtocols protocols)
    {
        void options(KestrelServerOptions serverOptions)
        {
            serverOptions.Listen(IPAddress.Loopback, port, listenOptions =>
            {
                listenOptions.UseHttps("mycert2.pfx", "password", options =>
                {
                    options.SslProtocols = SslProtocols.Tls12;
                });
                listenOptions.Protocols = protocols;
            });
        }

        handler.GivenThereIsAServiceRunningOnWithKestrelOptions(DownstreamUrl(port), basePath, options, async context =>
        {
            context.Response.StatusCode = 200;
            var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            await context.Response.WriteAsync(body);
        });
    }
}
