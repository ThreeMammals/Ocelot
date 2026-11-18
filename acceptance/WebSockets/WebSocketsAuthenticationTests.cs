using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Testing.Steps;
using System.Net.WebSockets;

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
    public async Task Should_reject_websocket_upgrade_with_401_when_no_token()
    {
        var ports = PortFinder.GetPorts(2);
        var route = GivenRoute(ports[0], "/", "/");
        route.DownstreamScheme = Uri.UriSchemeWs;
        route.AuthenticationOptions = new(JwtBearerDefaults.AuthenticationScheme);
        var gateway = new UriBuilder(Uri.UriSchemeWs, "localhost", ports[1]).Uri;
        using var ws = new ClientWebSocket();
        ws.Options.CollectHttpResponseDetails = true;
        await GivenWebSocketsServiceIsRunningAsync(ports[0], "/", EchoAsync);
        GivenThereIsAConfiguration(GivenConfiguration(route));
        await StartOcelotWithWebSockets(ports[1], WithJwtBearerAuthentication);
        await WhenIConnect(ws, gateway);
        ThenTheUpgradeIsRejected(ws);
    }

    private async Task WhenIConnect(ClientWebSocket ws, Uri gateway)
        => _connect = await Record.ExceptionAsync(() => ws.ConnectAsync(gateway, CancelMe));

    // An unauthenticated upgrade is rejected with 401 before reaching the WS proxy.
    private void ThenTheUpgradeIsRejected(ClientWebSocket ws)
    {
        _connect.ShouldBeOfType<WebSocketException>();
        ws.State.ShouldBe(WebSocketState.Closed);
        ws.HttpStatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        _connect.Message.ShouldBe("The server returned status code '401' when status code '101' was expected.");
    }
}
