using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ocelot.AcceptanceTests.WebSockets;

public sealed class ClientWebSocketTests : WebSocketsSteps
{
    private readonly ClientWebSocket _ws = new();
    public override void Dispose()
    {
        _ws.Dispose();
        base.Dispose();
    }

    /// <summary>It tests the following stack: HTTP 1.1, SSL, WebSocket.</summary>
    /// <returns>A <see cref="Task"/> object.</returns>
    [Theory]
    [InlineData("ws://corefx-net-http11.azurewebsites.net/WebSocket/EchoWebSocket.ashx", null)] // https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/websockets#differences-in-http11-and-http2-websockets
    [InlineData("wss://echo.websocket.org", "Request served by ")] // https://websocket.org/tools/websocket-echo-server/
    [InlineData("wss://ws.postman-echo.com/raw", null)] // https://blog.postman.com/introducing-postman-websocket-echo-service/
    public async Task Http11CLient_DirectConnection_ShouldConnect(string url, string expected)
    {
        var echoEndpoint = new Uri(url);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(3_500);
#if NET9_0_OR_GREATER
        // Keep-Alive strategy is PING/PONG
        // KeepAliveInterval is a positive finite TimeSpan, -AND-
        // KeepAliveTimeout is a positive finite TimeSpan
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);
        _ws.Options.KeepAliveTimeout = TimeSpan.FromSeconds(1);
#else
        // Keep-Alive strategy is Unsolicited PONG
        // KeepAliveInterval is a positive finite TimeSpan, -AND-
        // KeepAliveTimeout is TimeSpan.Zero or Timeout.InfiniteTimeSpan
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);
#endif
        await _ws.ConnectAsync(echoEndpoint, cts.Token);

        // Send test data
        var upload = Encoding.UTF8.GetBytes(Expected());
        await _ws.SendAsync(upload, WebSocketMessageType.Text, true, cts.Token);

        var echo = new byte[1024];
        var result = await _ws.ReceiveAsync(echo, cts.Token);
        string actual = Encoding.UTF8.GetString(echo, 0, result.Count);
        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, Expected() + " closed", cts.Token);

        if (string.IsNullOrEmpty(expected))
            Assert.Equal(Expected(), actual);
        else
            Assert.StartsWith(expected, actual);
    }

    /// <summary>It tests the following stack: HTTP/2, SSL, WebSocket.</summary>
    /// <remarks>HTTP/2 always requires an SSL certificate.</remarks>
    /// <returns>A <see cref="Task"/> object.</returns>
    //[Fact(Skip = $"TEST {nameof(Http20CLient_DirectConnection_ShouldConnect)} is skipped")]
    [Fact]
    public async Task Http20CLient_DirectConnection_ShouldConnect()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(3_500);

        int port = PortFinder.GetRandomPort();
        await GivenWebSocketsHttp2ServiceIsRunningAsync(port, EchoAsync, cts.Token);

        //var echoEndpoint = new Uri("wss://ws.postman-echo.com/raw");
        var echoEndpoint = new UriBuilder(Uri.UriSchemeWss, "localhost" /*"threemammals.com"*/, port).Uri;

#if NET9_0_OR_GREATER
        // Keep-Alive strategy is PING/PONG
        // KeepAliveInterval is a positive finite TimeSpan, -AND-
        // KeepAliveTimeout is a positive finite TimeSpan
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);
        _ws.Options.KeepAliveTimeout = TimeSpan.FromSeconds(1);
#else
        // Keep-Alive strategy is Unsolicited PONG
        // KeepAliveInterval is a positive finite TimeSpan, -AND-
        // KeepAliveTimeout is TimeSpan.Zero or Timeout.InfiniteTimeSpan
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);
#endif
        _ws.Options.HttpVersion = HttpVersion.Version20; // !!!
        _ws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        using var handler = new HttpClientHandler
        {
            // TODO Copilot prompt -> Linux Ubuntu How to add host (loopback address) to the local DNS subsystem?
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true, // see TODO should be commented
            PreAuthenticate = true,
            Credentials = new NetworkCredential("tom@threemammals.com", "password"),
        };
        using var invoker = new HttpMessageInvoker(handler);
        await _ws.ConnectAsync(echoEndpoint, invoker, cts.Token);

        // Send test data
        var upload = Encoding.UTF8.GetBytes(Expected());
        await _ws.SendAsync(upload, WebSocketMessageType.Text, true, cts.Token);

        var echo = new byte[1024];
        var result = await _ws.ReceiveAsync(echo, cts.Token);
        string actual = Encoding.UTF8.GetString(echo, 0, result.Count);
        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, Expected() + " closed", cts.Token);

        Assert.Equal(Expected(), actual);
    }

    private static string Expected([CallerMemberName] string testName = null)
        => testName ?? nameof(ClientWebSocketTests);
}
