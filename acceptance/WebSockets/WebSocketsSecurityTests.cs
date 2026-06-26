using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
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
    [Trait("PR", "2406")] // https://github.com/ThreeMammals/Ocelot/pull/2406
    public async Task Bug2403_UserScenario()
    {
        // Downstream service on 127.0.0.1:6001 (HTTP + WebSocket echo)
        var port = PortFinder.GetRandomPort();
        var downstream = new UriBuilder("http", "127.0.0.1", port);
        var downBuilder = WebApplication.CreateBuilder();
        downBuilder.WebHost.UseUrls(downstream.ToString());
        var down = downBuilder.Build();
        down.UseWebSockets();
        down.Use(async (ctx, next) =>
        {
            if (ctx.WebSockets.IsWebSocketRequest)
            {
                using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
                var buf = new byte[1024];
                var res = await ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
                var msg = Encoding.UTF8.GetString(buf, 0, res.Count);
                var reply = Encoding.UTF8.GetBytes("DOWNSTREAM-WS-ECHO:" + msg);
                await ws.SendAsync(new ArraySegment<byte>(reply), WebSocketMessageType.Text, true, CancellationToken.None);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                return;
            }
            await next();
        });
        down.MapGet("/{**rest}", () => "DOWNSTREAM-HTTP-OK");
        _ = down.RunAsync();

        // Ocelot gateway on 127.0.0.1:6000, route blocks 127.0.0.1
        var cfg = new FileConfiguration();
        cfg.Routes.Add(new FileRoute
        {
            UpstreamPathTemplate = "/proxy/{everything}",
            UpstreamHttpMethod = ["GET"],
            DownstreamPathTemplate = "/{everything}",
            DownstreamScheme = "http",
            DownstreamHostAndPorts = [ new(downstream.Host, downstream.Port) ],
            SecurityOptions = new() { IPBlockedList = [downstream.Host] },
        });

        var ocelotPort = PortFinder.GetRandomPort();
        var gwBuilder = WebApplication.CreateBuilder();
        gwBuilder.WebHost.UseUrls($"http://127.0.0.1:{ocelotPort}");
        gwBuilder.Configuration.AddOcelot(cfg);
        gwBuilder.Services.AddOcelot(gwBuilder.Configuration);
        var gw = gwBuilder.Build();
        await gw.UseOcelot();
        _ = gw.RunAsync();
        await Task.Delay(3000, CancelMe);

        // 1) Normal HTTP request from the blocked IP
        using var http = new HttpClient();
        var r = await http.GetAsync($"http://127.0.0.1:{ocelotPort}/proxy/anything", CancelMe);

        // Console.WriteLine($"[HTTP] status={(int)r.StatusCode}");
        r.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // 2) WebSocket upgrade request from the same blocked IP
        using var wsc = new ClientWebSocket();

        // await wsc.ConnectAsync(new Uri($"ws://127.0.0.1:{ocelotPort}/proxy/anything"), CancelMe);
        var ex = await Record.ExceptionAsync(()
            => wsc.ConnectAsync(new Uri($"ws://127.0.0.1:{ocelotPort}/proxy/anything"), CancelMe));
        ex.ShouldBeOfType<WebSocketException>();
        ex.Message.ShouldContain("403");
        ex.Message.ShouldBe("The server returned status code '403' when status code '101' was expected.");

        //var send = Encoding.UTF8.GetBytes("hello-from-blocked-ip");
        //await wsc.SendAsync(new ArraySegment<byte>(send), WebSocketMessageType.Text, true, CancelMe);
        //var rbuf = new byte[1024];
        //var rres = await wsc.ReceiveAsync(new ArraySegment<byte>(rbuf), CancelMe);
        //Console.WriteLine($"[WS]   reply=\"{Encoding.UTF8.GetString(rbuf, 0, rres.Count)}\"");
    }

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
