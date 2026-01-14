using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.Middleware;
using Ocelot.WebSockets;
using System.Net.WebSockets;
using System.Text;

namespace Ocelot.AcceptanceTests.WebSockets;

public class WebSocketsSteps : Steps
{
    private readonly WebSocketsFactory _factory = new();
    private readonly List<string> _secondRecieved = new();
    protected readonly List<string> _firstRecieved = new();

    protected IHost Host { get; private set; }

    public override void Dispose()
    {
        Host?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    protected static void WithConsole(WebHostBuilderContext context, ILoggingBuilder logging) => logging
        .AddConfiguration(context.Configuration.GetSection("Logging"))
        .AddConsole();
    protected static void WithConfiguration(WebHostBuilderContext hosting, IConfigurationBuilder config) => config
        .SetBasePath(hosting.HostingEnvironment.ContentRootPath);
    protected static void WithoutServices(IServiceCollection services) { }

    protected Task GivenWebSocketServiceIsRunningOnAsync(string url, Action<KestrelServerOptions> options, Func<HttpContext, Func<Task>, Task> middleware) =>
        handler.GivenThereIsAServiceRunningOnAsync(url,
            WithConfiguration,
            WithConsole,
            WithoutServices,
            app => app.UseWebSockets().Use(middleware),
            web => web.UseUrls(url).ConfigureKestrel(options).UseKestrel() // UseKestrelHttpsConfiguration()
        );

    protected void ThenBothDownstreamServicesAreCalled()
    {
        _firstRecieved.Count.ShouldBe(10);
        _firstRecieved.ForEach(x => x.ShouldBe("test"));
        _secondRecieved.Count.ShouldBe(10);
        _secondRecieved.ForEach(x => x.ShouldBe("chocolate"));
    }

    protected Task GivenWebSocketsServiceIsRunningAsync(int port, string path, Func<WebSocket, CancellationToken, Task> webSocketHandler, CancellationToken token)
    {
        async Task TheMiddleware(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path != path)
            {
                await next();
                return;
            }
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await webSocketHandler(webSocket, token);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
        void NoOptions(KestrelServerOptions options)
        {
        }
        var url = DownstreamUrl(port);
        return GivenWebSocketServiceIsRunningOnAsync(url, NoOptions, TheMiddleware);
    }

    protected Task GivenWebSocketsHttp2ServiceIsRunningAsync(int port, Func<WebSocket, CancellationToken, Task> webSocketHandler, CancellationToken token)
    {
        async Task TheMiddleware(HttpContext context, Func<Task> next)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await webSocketHandler(webSocket, token);
            }
            else
            {
                await next();
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
        void WithOptions(KestrelServerOptions options) => options.ListenAnyIP(port, WithHttp2);
        var url = DownstreamUrl(port, Uri.UriSchemeHttps);
        return GivenWebSocketServiceIsRunningOnAsync(url, WithOptions, TheMiddleware);
    }

    protected static async Task EchoAsync(WebSocket ws, CancellationToken token)
    {
        try
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result;
            while (true)
            {
                Array.Clear(buffer);
                result = await ws.ReceiveAsync(buffer, token);
                if (result.CloseStatus.HasValue)
                    break;
                var echo = new ArraySegment<byte>(buffer, 0, result.Count);
                await ws.SendAsync(echo, result.MessageType, result.EndOfMessage, token);
            }

            await ws.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, token);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected static async Task MessageAsync(WebSocket webSocket, CancellationToken token)
    {
        try
        {
            var buffer = new byte[1024 * 4];
            var bytes = Encoding.UTF8.GetBytes("chocolate");
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(bytes), result.MessageType, result.EndOfMessage, token);
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, token);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected async Task StartClient(Uri url)
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

    protected async Task StartSecondClient(Uri url)
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

    protected Task WhenIStartTheClients(int port)
    {
        var url = new UriBuilder(Uri.UriSchemeWs, "localhost", port).Uri;
        var firstClient = StartClient(url);
        var secondClient = StartSecondClient(url);
        return Task.WhenAll(firstClient, secondClient);
    }

    protected Task StartOcelotWithWebSockets(int port, Action<IServiceCollection> configureServices)
        => StartOcelotWithWebSockets(port, Uri.UriSchemeHttp, configureServices);
    protected async Task StartOcelotWithWebSockets(int port, string scheme, Action<IServiceCollection> configureServices)
    {
        var url = DownstreamUrl(port, scheme);
        void ConfigureWebHost(IWebHostBuilder b) => b
            .UseUrls(url)
            .ConfigureLogging(WithConsole);
        Host = await GivenOcelotHostIsRunning(WithBasicConfiguration, configureServices ?? WithAddOcelot, WithWebSockets, ConfigureWebHost);
    }

    protected static void WithWebSockets(IApplicationBuilder app)
        => app.UseWebSockets().UseOcelot().Wait();
    protected static void WithHttp2(ListenOptions options)
    {
        options.Protocols = HttpProtocols.Http2;
        options.UseHttps("mycert2.pfx", "password");
    }
    protected async Task StartHttp2OcelotWithWebSockets(int port)
    {
        void WithOptions(KestrelServerOptions o) => o.ListenAnyIP(port, WithHttp2);
        var url = DownstreamUrl(port, Uri.UriSchemeHttps);
        void ConfigureWebHost(IWebHostBuilder b) => b.UseUrls(url)
            .ConfigureLogging(WithConsole)
            .ConfigureKestrel(WithOptions).UseKestrel(); // UseKestrelHttpsConfiguration()
        Host = await GivenOcelotHostIsRunning(WithBasicConfiguration, WithAddOcelot, WithWebSockets, ConfigureWebHost);
    }
}
