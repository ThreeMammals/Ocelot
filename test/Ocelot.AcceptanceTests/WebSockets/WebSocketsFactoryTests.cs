using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Middleware;
using Ocelot.WebSockets;
using System.Net.WebSockets;
using System.Text;

namespace Ocelot.AcceptanceTests.WebSockets;

public sealed class WebSocketsFactoryTests : Steps
{
    private readonly List<string> _secondRecieved;
    private readonly List<string> _firstRecieved;
    private readonly WebSocketsFactory _factory;
    private IHost _ocelotHost;

    public WebSocketsFactoryTests() : base()
    {
        _firstRecieved = new List<string>();
        _secondRecieved = new List<string>();
        _factory = new WebSocketsFactory(); // concrete class, so, we perform real integration testing
    }

    public override void Dispose()
    {
        _ocelotHost.Dispose();
        base.Dispose();
    }

    [Fact]
    [Trait("Feat", "212")]
    [Trait("PR", "273")] // https://github.com/ThreeMammals/Ocelot/pull/273
    public async Task ShouldProxyWebsocketInputToDownstreamService()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute("/ws", port);
        var configuration = GivenConfiguration(route);
        GivenThereIsAConfiguration(configuration);
        int ocelotPort = PortFinder.GetRandomPort();
        var ocelotUrl = new UriBuilder(Uri.UriSchemeWs, "localhost", ocelotPort).Uri;
        await StartFakeOcelotWithWebSockets(ocelotPort);
        await StartFakeDownstreamService(DownstreamUrl(port), "/ws");
        await StartClient(ocelotUrl);
        ThenTheReceivedCountIs(10);

        void ThenTheReceivedCountIs(int count) => _firstRecieved.Count.ShouldBe(count);
    }

    [Fact]
    [Trait("Feat", "212")]
    [Trait("PR", "273")] // https://github.com/ThreeMammals/Ocelot/pull/273
    public void ShouldProxyWebsocketInputToDownstreamServiceAndUseLoadBalancer()
    {
        int port1 = PortFinder.GetRandomPort();
        int port2 = PortFinder.GetRandomPort();
        var route = GivenRoute("/ws", port1, port2);
        route.LoadBalancerOptions.Type = nameof(RoundRobin);
        var configuration = GivenConfiguration(route);
        int ocelotPort = PortFinder.GetRandomPort();
        this.Given(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => StartFakeOcelotWithWebSockets(ocelotPort))
            .And(_ => StartFakeDownstreamService(DownstreamUrl(port1), "/ws"))
            .And(_ => StartSecondFakeDownstreamService(DownstreamUrl(port2), "/ws"))
            .When(_ => WhenIStartTheClients(ocelotPort))
            .Then(_ => ThenBothDownstreamServicesAreCalled())
            .BDDfy();
    }

    private void ThenBothDownstreamServicesAreCalled()
    {
        _firstRecieved.Count.ShouldBe(10);
        _firstRecieved.ForEach(x => x.ShouldBe("test"));
        _secondRecieved.Count.ShouldBe(10);
        _secondRecieved.ForEach(x => x.ShouldBe("chocolate"));
    }

    private static FileRoute GivenRoute(string downstream = null, params int[] ports) => new()
    {
        UpstreamPathTemplate = "/",
        DownstreamPathTemplate = downstream ?? "/ws",
        DownstreamScheme = Uri.UriSchemeWs,
        DownstreamHostAndPorts = ports.Select(Localhost).ToList(),
    };

    private async Task StartFakeOcelotWithWebSockets(int port)
    {
        var url = new UriBuilder(Uri.UriSchemeHttp, "localhost", port).ToString();
        static void WithWebSockets(IApplicationBuilder app) => app.UseWebSockets().UseOcelot().Wait();
        void ConfigureWebHost(IWebHostBuilder b) => b
            .UseUrls(url)
            .ConfigureLogging((hosting, logging) => logging
                .AddConfiguration(hosting.Configuration.GetSection("Logging"))
                .AddConsole());
        _ocelotHost = await GivenOcelotHostIsRunning(WithBasicConfiguration, WithAddOcelot, WithWebSockets, ConfigureWebHost);
    }

    private Task WhenIStartTheClients(int port)
    {
        var url = new UriBuilder(Uri.UriSchemeWs, "localhost", port).Uri;
        var firstClient = StartClient(url);
        var secondClient = StartSecondClient(url);
        return Task.WhenAll(firstClient, secondClient);
    }

    private async Task StartClient(Uri url)
    {
        var client = _factory.CreateClient();
        await client.ConnectAsync(url, CancellationToken.None);

        var sending = Task.Run(async () =>
        {
            var line = "test";
            for (var i = 0; i < 10; i++)
            {
                var bytes = Encoding.UTF8.GetBytes(line);
                await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                await Task.Delay(10);
            }
            await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        });

        var receiving = Task.Run(async () =>
        {
            var buffer = new byte[1024 * 4];
            while (true)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    _firstRecieved.Add(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (client.State != WebSocketState.Closed)
                    {
                        // Last version, the client state is CloseReceived
                        // Valid states are: Open, CloseReceived, CloseSent
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                    break;
                }
            }
        });

        await Task.WhenAll(sending, receiving);
    }

    private async Task StartSecondClient(Uri url)
    {
        await Task.Delay(500);
        var client = _factory.CreateClient();
        await client.ConnectAsync(url, CancellationToken.None);

        var sending = Task.Run(async () =>
        {
            var line = "test";
            for (var i = 0; i < 10; i++)
            {
                var bytes = Encoding.UTF8.GetBytes(line);
                await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                await Task.Delay(10);
            }
            await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        });

        var receiving = Task.Run(async () =>
        {
            var buffer = new byte[1024 * 4];
            while (true)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    _secondRecieved.Add(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (client.State != WebSocketState.Closed)
                    {
                        // Last version, the client state is CloseReceived
                        // Valid states are: Open, CloseReceived, CloseSent
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                    break;
                }
            }
        });

        await Task.WhenAll(sending, receiving);
    }

    private Task StartFakeDownstreamService(string url, string path)
    {
        async Task TheMiddleware(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path == path)
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await Echo(webSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await next();
            }
        }
        return GivenWebSocketServiceIsRunningOnAsync(url, TheMiddleware);
    }

    private Task StartSecondFakeDownstreamService(string url, string path)
    {
        async Task The2ndMiddleware(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path == path)
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await Message(webSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await next();
            }
        }
        return GivenWebSocketServiceIsRunningOnAsync(url, The2ndMiddleware);
    }

    private static async Task Echo(WebSocket webSocket)
    {
        try
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static async Task Message(WebSocket webSocket)
    {
        try
        {
            var buffer = new byte[1024 * 4];
            var bytes = Encoding.UTF8.GetBytes("chocolate");
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(bytes), result.MessageType, result.EndOfMessage, CancellationToken.None);
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
