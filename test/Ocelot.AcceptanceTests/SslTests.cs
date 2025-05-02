using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public sealed class SslTests : Steps
{
    private string _downstreamPath;
    private readonly ServiceHandler _serviceHandler;

    public SslTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    [Fact]
    public void Should_dangerous_accept_any_server_certificate_validator()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "https",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        DangerousAcceptAnyServerCertificateValidator = true,
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", 200, "Hello from Laura", port))
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
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "https",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        DangerousAcceptAnyServerCertificateValidator = false,
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", 200, "Hello from Laura", port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody, int port)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, "mycert.pfx", "password", port, async context =>
        {
            _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

            if (_downstreamPath != basePath)
            {
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync("downstream path didnt match base path");
            }
            else
            {
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(responseBody);
            }
        });
    }

    public override void Dispose()
    {
        _serviceHandler?.Dispose();
        base.Dispose();
    }
}
