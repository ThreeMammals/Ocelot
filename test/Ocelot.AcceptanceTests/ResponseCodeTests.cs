using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public sealed class ResponseCodeTests : Steps
{
    private readonly ServiceHandler _serviceHandler;

    public ResponseCodeTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    [Fact]
    public void ShouldReturnResponse304WhenServiceReturns304()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/{everything}",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/{everything}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/inline.132.bundle.js", 304))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/inline.132.bundle.js"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotModified))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, (context) => Task.Run(() =>
        {
            context.Response.StatusCode = statusCode;
        }));
    }

    public override void Dispose()
    {
        _serviceHandler?.Dispose();
        base.Dispose();
    }
}
