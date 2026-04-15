
namespace Ocelot.AcceptanceTests.Requester;

[Trait("Bug", "2376")] // https://github.com/ThreeMammals/Ocelot/issues/2376
[Trait("PR", "2379")] // https://github.com/ThreeMammals/Ocelot/pull/2379
public sealed class InvalidHeaderValueTests : Steps
{
    [Theory]
    [InlineData("skull", "-=💀=-", HttpStatusCode.BadRequest)] // original bug 2374
    [InlineData("utf8char", "-=é=-", HttpStatusCode.BadRequest)]
    [InlineData("utf16char", "-=漢=-", HttpStatusCode.BadRequest)]
    [InlineData("ascii", "valid-ascii", HttpStatusCode.OK)]
    public void Should_return_400_BadRequest_having_non_ascii_header_value_otherwise_200_OK(
        string headerName, string headerValue, HttpStatusCode expectedStatus)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/ocelot/posts/{id}", "/todos/{id}");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOnPath(port, "/todos/askdj", "Hello from Laura Demkowicz-Duffy"))
            .And(x => x.GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunning())
            .And(x => x.GivenIAddAHeader(headerName, headerValue))
            .When(x => x.WhenIGetUrlOnTheApiGatewaySafely("/ocelot/posts/askdj"))
            .Then(x => x.ThenTheStatusCodeShouldBe(expectedStatus))
            .And(x => ThenTheResponseBodyShouldBe(expectedStatus == HttpStatusCode.OK ? "Hello from Laura Demkowicz-Duffy" : string.Empty))
            .BDDfy();
    }

    private async Task WhenIGetUrlOnTheApiGatewaySafely(string url)
    {
        try
        {
            await WhenIGetUrlOnTheApiGateway(url);
        }
        catch (HttpRequestException)
        {
            response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        }
    }
}
