using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Testing.Steps;
using System.Net.WebSockets;
using System.Text;

namespace Ocelot.Acceptance.WebSockets;

public sealed class WebSocketsHeadersTests : WebSocketsSteps
{
    private string requestId;
    private Exception _connect;

    private async Task TheMiddleware(HttpContext context, Func<Task> next)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            requestId = Convert.ToString(context.Request.Headers["X-Request-Id"]);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await EchoAsync(webSocket, context.RequestAborted);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }

    [Fact]
    [Trait("Bug", "2422")] // https://github.com/ThreeMammals/Ocelot/issues/2422
    public void Should_forward_request_id_header_to_websocket_downstream()
    {
        var ports = PortFinder.GetPorts(2);
        var route = GivenRoute(ports[0], "/", "/");
        route.DownstreamScheme = Uri.UriSchemeWs;
        route.RequestIdKey = "X-Request-Id";
        var gateway = new UriBuilder(Uri.UriSchemeWs, "localhost", ports[1]).Uri;
        using var ws = new ClientWebSocket();
        ws.Options.CollectHttpResponseDetails = true; // capture the handshake status so HttpStatusCode is populated
        this.Given(_ => GivenWebSocketServiceIsRunningOnAsync(DownstreamUrl(ports[0]), NoOptions, TheMiddleware))
            .And(_ => GivenThereIsAConfiguration(GivenConfiguration(route)))
            .And(_ => StartOcelotWithWebSockets(ports[1], null))
            .When(_ => WhenIConnect(ws, gateway))
            .Then(_ => ThenTheUpgradeSucceeds(ws))
            .And(_ => ThenTheDownstreamEchoesMessage(ws))
            .And(_ => ThenTheWsClientHasBeenClosedSuccessfully(ws))
            .And(_ => ThenTheDownstreamReceivedTheRequestId())
        .BDDfy();
    }

    private async Task WhenIConnect(ClientWebSocket ws, Uri gateway)
        => _connect = await Record.ExceptionAsync(() => ws.ConnectAsync(gateway, CancelMe));

    private static void NoOptions(KestrelServerOptions options) { }

    // An allowed IP passes SecurityMiddleware, so the upgrade completes (101) and ConnectAsync does not throw.
    private void ThenTheUpgradeSucceeds(ClientWebSocket ws)
    {
        _connect.ShouldBeNull();
        ws.State.ShouldBe(WebSocketState.Open);
    }
    private static void ThenTheWsClientHasBeenClosedSuccessfully(ClientWebSocket ws)
    {
        ws.State.ShouldBe(WebSocketState.Closed);
        ws.CloseStatus.ShouldBe(WebSocketCloseStatus.NormalClosure);
        ws.HttpStatusCode.ShouldBe(HttpStatusCode.SwitchingProtocols); // ws.Options.CollectHttpResponseDetails
    }

    // Security passed and the request reached the WS proxy: a round-trip echo confirms end-to-end proxying still works.
    private async Task ThenTheDownstreamEchoesMessage(ClientWebSocket ws)
    {
        const string message = "allowed-ip-probe";
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancelMe);
        cts.CancelAfter(TimeSpan.FromSeconds(3)); // guard against an indefinite receive hang
        await ws.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cts.Token);
        var buffer = new byte[256];
        var result = await ws.ReceiveAsync(buffer, cts.Token);
        Encoding.UTF8.GetString(buffer, 0, result.Count).ShouldBe(message);
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cts.Token);
    }

    private void ThenTheDownstreamReceivedTheRequestId()
    {
        requestId.ShouldNotBeNullOrEmpty("Ocelot's RequestIdMiddleware header should be forwarded to the WebSocket downstream");
    }
}
