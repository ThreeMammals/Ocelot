using System.Net;
using System.Net.WebSockets;
using System.Text;
using Ocelot.Testing.Steps;

namespace Ocelot.Acceptance.WebSockets;

public sealed class WebSocketsRateLimitingTests : WebSocketsSteps
{
    private Exception _connect;

    [Fact]
    [Trait("Bug", "2422")] // https://github.com/ThreeMammals/Ocelot/issues/2422
    public void Should_reject_websocket_upgrade_with_429_when_rate_limit_exceeded()
    {
        var ports = PortFinder.GetPorts(2);
        var route = GivenRoute(ports[0], "/", "/");
        route.DownstreamScheme = Uri.UriSchemeWs;
        route.RateLimitOptions = new()
        {
            Limit = 2,
            Period = "1m",
            Wait = "1m",
            StatusCode = 429,
        };
        var gateway = new UriBuilder(Uri.UriSchemeWs, "localhost", ports[1]).Uri;
        using var ws1 = CreateClient();
        using var ws2 = CreateClient();
        using var ws3 = CreateClient();
        this.Given(_ => GivenWebSocketsServiceIsRunningAsync(ports[0], "/", EchoAsync))
            .And(_ => GivenThereIsAConfiguration(GivenConfiguration(route)))
            .And(_ => StartOcelotWithWebSockets(ports[1], null))
            .When(_ => WhenIConnect(ws1, gateway))
            .Then(_ => ThenTheUpgradeSucceeds(ws1))
            .And(_ => ThenTheDownstreamEchoesMessage(ws1))
            .And(_ => ThenTheWsClientHasBeenClosedSuccessfully(ws1))
            .When(_ => WhenIConnect(ws2, gateway))
            .Then(_ => ThenTheUpgradeSucceeds(ws2))
            .And(_ => ThenTheDownstreamEchoesMessage(ws2))
            .And(_ => ThenTheWsClientHasBeenClosedSuccessfully(ws2))
            .When(_ => WhenIConnect(ws3, gateway))
            .Then(_ => ThenTheUpgradeIsRejected(ws3))
        .BDDfy();
    }

    private ClientWebSocket CreateClient()
    {
        var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Oc-Client", "ws-test-client");
        ws.Options.CollectHttpResponseDetails = true;
        return ws;
    }

    private async Task WhenIConnect(ClientWebSocket ws, Uri gateway)
        => _connect = await Record.ExceptionAsync(() => ws.ConnectAsync(gateway, CancelMe));

    private void ThenTheUpgradeSucceeds(ClientWebSocket ws)
    {
        _connect.ShouldBeNull();
        ws.State.ShouldBe(WebSocketState.Open);
    }
    private void ThenTheUpgradeIsRejected(ClientWebSocket ws)
    {
        _connect.ShouldBeOfType<WebSocketException>();
        ws.State.ShouldBe(WebSocketState.Closed);
        ws.HttpStatusCode.ShouldBe(HttpStatusCode.TooManyRequests); // ws.Options.CollectHttpResponseDetails
        _connect.Message.ShouldBe("The server returned status code '429' when status code '101' was expected.");
    }
    private static void ThenTheWsClientHasBeenClosedSuccessfully(ClientWebSocket ws)
    {
        ws.State.ShouldBe(WebSocketState.Closed);
        ws.CloseStatus.ShouldBe(WebSocketCloseStatus.NormalClosure);
        ws.HttpStatusCode.ShouldBe(HttpStatusCode.SwitchingProtocols); // ws.Options.CollectHttpResponseDetails
    }

    private async Task ThenTheDownstreamEchoesMessage(ClientWebSocket ws)
    {
        const string message = "rate-limit-probe";
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancelMe);
        cts.CancelAfter(TimeSpan.FromSeconds(3)); // guard against an indefinite receive hang
        await ws.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cts.Token);
        var buffer = new byte[256];
        var result = await ws.ReceiveAsync(buffer, cts.Token);
        Encoding.UTF8.GetString(buffer, 0, result.Count).ShouldBe(message);
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cts.Token);
    }
}
