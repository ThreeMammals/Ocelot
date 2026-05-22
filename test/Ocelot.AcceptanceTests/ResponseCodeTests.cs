namespace Ocelot.AcceptanceTests;

public sealed class ResponseCodeTests : Steps
{
    /// <summary>
    /// TODO: Move the test to the Headers Transform namespace since the bug was related to the Headers Transform middleware.
    /// </summary>
    [Fact]
    [Trait("Bug", "440")] // https://github.com/ThreeMammals/Ocelot/issues/440
    [Trait("PR", "450")] // https://github.com/ThreeMammals/Ocelot/pull/450
    public void ShouldReturnResponse304WhenServiceReturns304()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenCatchAllRoute(port);
        var configuration = GivenConfiguration(route);
        var body = Body();
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.NotModified, body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/inline.132.bundle.js"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotModified))
            .Then(x => ThenTheResponseBodyShouldBeEmpty())
        .BDDfy();
    }
}
