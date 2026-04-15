using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using System.Net;
using System.Net.Http;
using TestStack.BDDfy;

namespace Ocelot.AcceptanceTests.Requester;

[Trait("Bug", "2376")]
[Trait("PR", "2381")]
public sealed class InvalidHeaderValueTests : Steps
{
    private const string GatewayRequestPath = "/ocelot/posts/askdj";
    private const string DownstreamRequestPath = "/todos/askdj";
    private const string DownstreamResponseBody = "Hello from Laura";
    private const string TestHeaderName = "skull";

    [Theory]
    [InlineData("💀", HttpStatusCode.BadRequest)]
    [InlineData("é", HttpStatusCode.BadRequest)]
    [InlineData("漢", HttpStatusCode.BadRequest)]
    [InlineData("valid-ascii", HttpStatusCode.OK)]
    public void Should_return_expected_status_code_when_request_contains_header_value(string headerValue, HttpStatusCode expectedStatusCode)
    {
        var downstreamPort = PortFinder.GetRandomPort();
        var route = GivenRoute(downstreamPort, "/ocelot/posts/{id}", "/todos/{id}");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOnPath(downstreamPort, DownstreamRequestPath, DownstreamResponseBody))
            .And(x => x.GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunning())
            .And(x => x.GivenIAddAHeader(TestHeaderName, headerValue))
            .When(x => x.WhenIGetUrlOnTheApiGatewaySafely(GatewayRequestPath))
            .Then(x => x.ThenTheStatusCodeShouldBe(expectedStatusCode))
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
