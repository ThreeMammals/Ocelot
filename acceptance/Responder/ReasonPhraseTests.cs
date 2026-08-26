using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Ocelot.Acceptance.Responder;

public sealed class ReasonPhraseTests : Steps
{
    [Fact]
    [Trait("Bug", "599")] // https://github.com/ThreeMammals/Ocelot/issues/599
    [Trait("PR", "618")] // https://github.com/ThreeMammals/Ocelot/pull/618
    public void Should_return_reason_phrase()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, "/", "some reason"))
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
