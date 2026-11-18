namespace Ocelot.Acceptance.Transformations;

public sealed class ResponseCodeTests : Steps
{
    [Fact]
    [Trait("Bug", "440")] // https://github.com/ThreeMammals/Ocelot/issues/440
    [Trait("PR", "450")] // https://github.com/ThreeMammals/Ocelot/pull/450
    public async Task ShouldReturnResponse304WhenServiceReturns304()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenCatchAllRoute(port);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.NotModified);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/inline.132.bundle.js");
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotModified);
        await ThenTheResponseBodyShouldBeEmpty();
    }
}
