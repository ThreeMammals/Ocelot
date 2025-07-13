using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Ocelot.AcceptanceTests;

public sealed class ReasonPhraseTests : Steps
{
    public ReasonPhraseTests() { }

    [Fact]
    public void Should_return_reason_phrase()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", "some reason"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .And(_ => ThenTheResponseReasonPhraseIs("some reason"))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, string reasonPhrase)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, context =>
        {
            context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = reasonPhrase;
            return context.Response.WriteAsync("YOYO!");
        });
    }
}
