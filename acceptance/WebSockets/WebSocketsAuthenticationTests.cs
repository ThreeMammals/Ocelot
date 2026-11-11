using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Testing.Steps;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using System.Net.WebSockets;
using System.Text;

namespace Ocelot.Acceptance.WebSockets;

public sealed class WebSocketsAuthenticationTests : WebSocketsSteps
{
    private Exception _connect;

    private void WithJwtBearerAuthentication(IServiceCollection services)
    {
        services.AddOcelot();
        services.AddAuthentication().AddJwtBearer();
    }

    [Fact]
    [Trait("Bug", "2422")] // https://github.com/ThreeMammals/Ocelot/issues/2422
    public void Should_reject_websocket_upgrade_with_401_when_no_token()
    {
        var ports = PortFinder.GetPorts(2);
        var route = GivenRoute(ports[0], "/", "/");
        route.DownstreamScheme = Uri.UriSchemeWs;
        route.AuthenticationOptions = new(JwtBearerDefaults.AuthenticationScheme);
        var gateway = new UriBuilder(Uri.UriSchemeWs, "localhost", ports[1]).Uri;
        using var ws = new ClientWebSocket();
        ws.Options.CollectHttpResponseDetails = true;
        this.Given(_ => GivenWebSocketsServiceIsRunningAsync(ports[0], "/", EchoAsync))
            .And(_ => GivenThereIsAConfiguration(GivenConfiguration(route)))
            .And(_ => StartOcelotWithWebSockets(ports[1], WithJwtBearerAuthentication))
            .When(_ => WhenIConnect(ws, gateway))
            .Then(_ => ThenTheUpgradeIsRejected(ws))
        .BDDfy();
    }

    private async Task WhenIConnect(ClientWebSocket ws, Uri gateway)
        => _connect = await Record.ExceptionAsync(() => ws.ConnectAsync(gateway, CancelMe));

    private static void NoOptions(KestrelServerOptions options) { }

    // An unauthenticated upgrade is rejected with 401 before reaching the WS proxy.
    private void ThenTheUpgradeIsRejected(ClientWebSocket ws)
    {
        _connect.ShouldBeOfType<WebSocketException>();
        ws.State.ShouldBe(WebSocketState.Closed);
        ws.HttpStatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        _connect.Message.ShouldBe("The server returned status code '401' when status code '101' was expected.");
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
}
