using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Ocelot.Configuration.File;
using System.Security.Authentication;

namespace Ocelot.Acceptance.Configuration;

[Trait("PR", "1127")] // https://github.com/ThreeMammals/Ocelot/pull/1127
[Trait("Feat", "1124")] // https://github.com/ThreeMammals/Ocelot/issues/1124
public sealed class DownstreamHttpVersionTests : Steps
{
    [Fact]
    public async Task Should_return_response_200_when_using_http_one()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, Uri.UriSchemeHttp, HttpVersion.Version10);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, "/", HttpProtocols.Http1);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_return_response_200_when_using_http_one_point_one()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, Uri.UriSchemeHttp, HttpVersion.Version11);
        route.DangerousAcceptAnyServerCertificateValidator = true;
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, "/", HttpProtocols.Http1);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_return_response_200_when_using_http_two_point_zero()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, Uri.UriSchemeHttps, HttpVersion.Version20);
        route.DangerousAcceptAnyServerCertificateValidator = true;
        var configuration = GivenConfiguration(route);

        const string expected = "here is some content";
        GivenThereIsAServiceUsingHttpsRunningOn(port, "/", HttpProtocols.Http2);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/", expected);
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(expected);
    }

    [Fact]
    public async Task Should_return_response_502_when_using_http_one_to_talk_to_server_running_http_two()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, Uri.UriSchemeHttps, HttpVersion.Version11);
        route.DangerousAcceptAnyServerCertificateValidator = true;
        var configuration = GivenConfiguration(route);

        const string expected = "here is some content";
        GivenThereIsAServiceRunningOn(port, "/", HttpProtocols.Http2);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/", expected);
        ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway);
    }

    //TODO: does this test make any sense?
    [Fact]
    public async Task Should_return_response_200_when_using_http_two_to_talk_to_server_running_http_one_point_one()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, Uri.UriSchemeHttp, HttpVersion.Version11);
        route.DangerousAcceptAnyServerCertificateValidator = true;
        var configuration = GivenConfiguration(route);

        const string expected = "here is some content";
        GivenThereIsAServiceRunningOn(port, "/", HttpProtocols.Http1);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/", expected);
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(expected);
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
