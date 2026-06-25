using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Ocelot.Middleware;
using Ocelot.Testing.Steps;
using System.Net.WebSockets;
using System.Text;

namespace Ocelot.AcceptanceTests.WebSockets;

public sealed class WebSocketsSecurityTests : WebSocketsSteps
{
    private Exception _connect;

    [Fact]
    [Trait("Bug", "2403")] // https://github.com/ThreeMammals/Ocelot/issues/2403
    public void Should_block_websocket_upgrade_for_blocked_ip()
    {
        const string ip = "192.168.1.1";
        var ports = PortFinder.GetPorts(2);
        var route = GivenRoute(ports[0], "/", "/");
        route.DownstreamScheme = Uri.UriSchemeWs;
        route.SecurityOptions = new() { IPBlockedList = [ip] };
        var gateway = new UriBuilder(Uri.UriSchemeWs, "localhost", ports[1]).Uri;
        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader(ForwardedHeadersDefaults.XForwardedForHeaderName, ip);
        ws.Options.CollectHttpResponseDetails = true; // capture the handshake status so HttpStatusCode is populated
        this.Given(_ => GivenWebSocketsServiceIsRunningAsync(ports[0], "/", EchoAsync))
            .And(_ => GivenThereIsAConfiguration(GivenConfiguration(route)))
            .And(_ => StartOcelotBehindForwardedHeaders(ports[1]))
            .When(_ => WhenIConnect(ws, gateway))
            .Then(_ => ThenTheUpgradeIsRejected(ws))
        .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2403")] // https://github.com/ThreeMammals/Ocelot/issues/2403
    public void Should_allow_websocket_upgrade_for_allowed_ip()
    {
        const string ip = "192.168.1.1";
        var ports = PortFinder.GetPorts(2);
        var route = GivenRoute(ports[0], "/", "/");
        route.DownstreamScheme = Uri.UriSchemeWs;
        route.SecurityOptions = new() { IPAllowedList = [ip] };
        var gateway = new UriBuilder(Uri.UriSchemeWs, "localhost", ports[1]).Uri;
        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader(ForwardedHeadersDefaults.XForwardedForHeaderName, ip);
        ws.Options.CollectHttpResponseDetails = true; // capture the handshake status so HttpStatusCode is populated
        this.Given(_ => GivenWebSocketsServiceIsRunningAsync(ports[0], "/", EchoAsync))
            .And(_ => GivenThereIsAConfiguration(GivenConfiguration(route)))
            .And(_ => StartOcelotBehindForwardedHeaders(ports[1]))
            .When(_ => WhenIConnect(ws, gateway))
            .Then(_ => ThenTheUpgradeSucceeds(ws))
            .And(_ => ThenTheDownstreamEchoesMessage(ws))
            .And(_ => ThenTheWsClientHasBeenClosedSuccessfully(ws))
        .BDDfy();
    }

    private async Task WhenIConnect(ClientWebSocket ws, Uri gateway)
        => _connect = await Record.ExceptionAsync(() => ws.ConnectAsync(gateway, CancelMe));

    // Per RFC 6455 (§4.1), a declined upgrade is answered with a non-101 HTTP status (403 Forbidden here),
    // so ConnectAsync throws WebSocketException and the client reads the status via HttpStatusCode.
    private void ThenTheUpgradeIsRejected(ClientWebSocket ws)
    {
        _connect.ShouldBeOfType<WebSocketException>();
        ws.State.ShouldBe(WebSocketState.Closed);
        ws.HttpStatusCode.ShouldBe(HttpStatusCode.Forbidden); // ws.Options.CollectHttpResponseDetails
        _connect.Message.ShouldBe("The server returned status code '403' when status code '101' was expected.");
    }

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

    private Task StartOcelotBehindForwardedHeaders(int port)
    {
        var url = DownstreamUrl(port);
        return GivenOcelotHostIsRunning(WithBasicConfiguration, WithAddOcelot,
            WithForwardedHeaders, null, b => b.UseUrls(url), null);
    }

    private static void WithForwardedHeaders(IApplicationBuilder app)
    {
        app.UseForwardedHeaders(new() { ForwardedHeaders = ForwardedHeaders.XForwardedFor });
        app.UseOcelot().Wait(); // internally calls UseWebSockets() since v25.0
    }
}
