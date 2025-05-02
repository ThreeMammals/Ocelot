using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public sealed class ReasonPhraseTests : Steps
{
    private readonly ServiceHandler _serviceHandler;

    public ReasonPhraseTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    [Fact]
    public void Should_return_reason_phrase()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
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
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", "some reason"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .And(_ => ThenTheReasonPhraseIs("some reason"))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, string reasonPhrase)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
        {
            context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = reasonPhrase;

            await context.Response.WriteAsync("YOYO!");
        });
    }

    public override void Dispose()
    {
        _serviceHandler?.Dispose();
        base.Dispose();
    }
}
