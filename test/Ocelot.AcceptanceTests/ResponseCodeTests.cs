using Microsoft.AspNetCore.Http;

namespace Ocelot.AcceptanceTests;

public sealed class ResponseCodeTests : Steps
{
    public ResponseCodeTests()
    {
    }

    [Fact]
    public void ShouldReturnResponse304WhenServiceReturns304()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenCatchAllRoute(port);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/inline.132.bundle.js", HttpStatusCode.NotModified))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/inline.132.bundle.js"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotModified))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, context =>
        {
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(statusCode.ToString());
        });
    }
}
