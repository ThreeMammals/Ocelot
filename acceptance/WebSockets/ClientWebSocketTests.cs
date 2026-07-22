using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.AcceptanceTests.Logging;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Testing.Steps;
using Ocelot.WebSockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Ocelot.AcceptanceTests.WebSockets;

public sealed class ClientWebSocketTests : WebSocketsSteps
{
    private readonly ClientWebSocket _ws = new();
    private readonly CancellationTokenSource _cts = new();
    private string _echo;

    public ClientWebSocketTests()
    {
        _cts.CancelAfter(3_500); // run (wait) all tests no more than 3.5 seconds
    }

    public override void Dispose()
    {
        _ws.Dispose();
        _cts.Dispose();
        base.Dispose();
    }

    public override CancellationToken CancelMe => _cts.Token;

    /// <summary>It tests the following stack: HTTP 1.1, SSL, WebSocket.</summary>
    /// <returns>A <see cref="Task"/> object.</returns>
    [Theory]
    [InlineData("ws://corefx-net-http11.azurewebsites.net/WebSocket/EchoWebSocket.ashx", null)] // https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/websockets#differences-in-http11-and-http2-websockets
    [InlineData("wss://echo.websocket.org", "Request served by ")] // https://websocket.org/tools/websocket-echo-server/
    [InlineData("wss://ws.postman-echo.com/raw", null)] // https://blog.postman.com/introducing-postman-websocket-echo-service/
    public void Http11ClientWhenDirectConnectionThenShouldConnect(string url, string expected)
    {
        Assert.SkipWhen(IsCiCd(), "Skipped in CI/CD! Running the test in a local development environment is sufficient to ensure the quality of the related WebSocket features.");

        var body = Body();
        var echoEndpoint = new Uri(url);
        this
            .Given(x => GivenOptions())
            .And(x => _ws.ConnectAsync(echoEndpoint, CancelMe))
            .When(x => WhenISendAndReceiveEchoMessage(body))
            .ThenIf(string.IsNullOrEmpty(expected), x => ThenEchoShouldBe(Expected(body)))
            .ThenIfNot(string.IsNullOrEmpty(expected), x => ThenEchoShouldStartWith(expected))
        .BDDfy();
    }

    /// <summary>It tests the following stack: HTTP/2, SSL, WebSocket.</summary>
    /// <remarks>HTTP/2 always requires an SSL certificate.</remarks>
    /// <returns>A <see cref="Task"/> object.</returns>
    [Fact]
    public void Http20ClientWhenDirectConnectionThenShouldConnect()
    {
        int port = PortFinder.GetRandomPort();
        var echoEndpoint = new UriBuilder(Uri.UriSchemeWss, /*"localhost"*/ "threemammals.com", port).Uri;
        using var handler = new HttpClientHandler
        {
            // ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
            PreAuthenticate = false,
            Credentials = new NetworkCredential("tom@threemammals.com", "password"),
        };
        if (IsCiCd() && RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // MacOS
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true; // TODO Makes sense to log in console
        using var invoker = new HttpMessageInvoker(handler);
        var body = Body();
        this
            .Given(x => GivenHttp2Options())
            .And(x => GivenWebSocketsHttp2ServiceIsRunningAsync(port, EchoAsync))
            .And(x => _ws.ConnectAsync(echoEndpoint, invoker, CancelMe))
            .When(x => WhenISendAndReceiveEchoMessage(body))
            .Then(x => ThenEchoShouldBe(Expected(body)))
        .BDDfy();
    }

    ///// <summary>In the middle, Ocelot tests the following stack: HTTP 1.1, SSL, WebSocket.</summary>
    ///// <returns>A <see cref="Task"/> object.</returns>
    [Theory]
    [InlineData("ws", "corefx-net-http11.azurewebsites.net", 80, "/WebSocket/EchoWebSocket.ashx", null)] // https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/websockets#differences-in-http11-and-http2-websockets
    [InlineData("wss","echo.websocket.org", 443, "/", "Request served by ")] // https://websocket.org/tools/websocket-echo-server/
    [InlineData("wss", "ws.postman-echo.com", 443, "/raw", null)] // https://blog.postman.com/introducing-postman-websocket-echo-service/
    public void Http11ClientWhenOcelotInTheMiddleThenShouldConnect(string scheme, string host, int port, string path, string expected)
    {
        Assert.SkipWhen(IsCiCd(), "Skipped in CI/CD! Running the test in a local development environment is sufficient to ensure the quality of the related WebSocket features.");

        var route = GivenWsRoute(scheme, host, port, path);
        var configuration = GivenConfiguration(route);
        int ocelotPort = PortFinder.GetRandomPort();
        var ocelot = new UriBuilder(Uri.UriSchemeWs, "localhost", ocelotPort).Uri;
        var body = Body();
        this
            .Given(x => GivenOptions())
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => StartOcelotWithWebSockets(ocelotPort, WithAddOcelot))
            .And(x => _ws.ConnectAsync(ocelot, CancelMe))
            .When(x => WhenISendAndReceiveEchoMessage(body))
            .ThenIf(string.IsNullOrEmpty(expected), x => ThenEchoShouldBe(Expected(body)))
            .ThenIfNot(string.IsNullOrEmpty(expected), x => ThenEchoShouldStartWith(expected))
        .BDDfy();
    }

    /// <summary>In the middle, Ocelot tests the following stack: HTTP/2, SSL, WebSocket.</summary>
    /// <remarks>HTTP/2 always requires an SSL certificate.<br/>
    /// TODO: Scenario of HTTP/2 (SSL) vs WebSocket is not supported by Ocelot's <see cref="WebSocketsProxyMiddleware"/>: see the ConnectAsync method.
    /// </remarks>
    /// <returns>A <see cref="Task"/> object.</returns>
    // Scenario of HTTP/2 (SSL) vs WebSocket is not supported by Ocelot's WebSocketsProxyMiddleware.
    // AI Q.1: websocket http/2 | What browsers and tools support this couple?
    // AI A.1: Proxy Servers: Implementing WebSocket support for HTTP/2 proxies requires handling the CONNECT request with a ':protocol' pseudo-header.
    //         See Stack Overflow | Implementing websocket support for HTTP/2 proxies -> https://stackoverflow.com/questions/65129151/implementing-websocket-support-for-http-2-proxies
    // AI Q.2: C# Yarp | Does Yarp support webSocket+HTTP/2 forwarding?
    // AI A.2: Yes! YARP (Yet Another Reverse Proxy) supports WebSockets over HTTP/2 starting in .NET 7 and YARP 2.0.
    //         See MS Learn | YARP Proxying WebSockets and SPDY -> https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/websockets
    [Fact(DisplayName = "TODO " + nameof(Http20ClientWhenOcelotInTheMiddleThenShouldConnect),
        Skip = "TODO: HTTP/2 (SSL) vs WebSocket is unsupported scenario by Ocelot Core currently, unfortunately...")]
    public void Http20ClientWhenOcelotInTheMiddleThenShouldConnect()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenWsRoute(Uri.UriSchemeWss, /*"localhost"*/ "threemammals.com", port);
        route.DownstreamHttpVersion = HttpVersion.Version20.ToString(); // 2.0 !!!
        route.DownstreamHttpVersionPolicy = nameof(HttpVersionPolicy.RequestVersionOrHigher);
        var configuration = GivenConfiguration(route);
        int ocelotPort = PortFinder.GetRandomPort();
        var ocelot = new UriBuilder(Uri.UriSchemeWss, "localhost", ocelotPort).Uri;
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
        };
        using var invoker = new HttpMessageInvoker(handler);
        var body = Body();
        this
            .Given(x => GivenWebSocketsHttp2ServiceIsRunningAsync(port, EchoAsync))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => StartHttp2OcelotWithWebSockets(ocelotPort))
            .And(x => GivenHttp2Options())
            .And(x => _ws.ConnectAsync(ocelot, invoker, CancelMe)) // TODO System.Net.WebSockets.WebSocketException : The server returned status code '500' when status code '200' was expected.
            .When(x => WhenISendAndReceiveEchoMessage(body))
            .Then(x =>  ThenEchoShouldBe(Expected(body)))
        .BDDfy();
    }

    [Theory]
    [Trait("Bug", "930")] // https://github.com/ThreeMammals/Ocelot/issues/930
    [Trait("PR", "2091")] // https://github.com/ThreeMammals/Ocelot/pull/2091
    [InlineData("ws", "corefx-net-http11.azurewebsites.net", 80, "/WebSocket/EchoWebSocket.ashx")] // https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/websockets#differences-in-http11-and-http2-websockets
    /*[InlineData("wss", "echo.websocket.org", 443, "/")] // https://websocket.org/tools/websocket-echo-server/ */
    [InlineData("wss", "ws.postman-echo.com", 443, "/raw")] // https://blog.postman.com/introducing-postman-websocket-echo-service/
    public void Http11ClientWhenConnectionClosedPrematurelyThenShouldCloseSocketsWithoutExceptions(string scheme, string host, int port, string path)
    {
        Assert.SkipWhen(IsCiCd(), "Skipped in CI/CD! Running the test in a local development environment is sufficient to ensure the quality of the related WebSocket features.");

        Action<IServiceCollection> WithExtraLogging = (services) =>
            services.AddOcelot()
            .Services.RemoveAll<IOcelotLoggerFactory>()
            .AddSingleton<IOcelotLoggerFactory, TestLoggerFactory<ClientWebSocketTests>>();

        var route = GivenWsRoute(scheme, host, port, path);
        var configuration = GivenConfiguration(route);
        int ocelotPort = PortFinder.GetRandomPort();
        var ocelot = new UriBuilder(Uri.UriSchemeWs, "localhost", ocelotPort).Uri;
        var body = Expected();
        var upload = Encoding.ASCII.GetBytes(body);
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => StartOcelotWithWebSockets(ocelotPort, WithExtraLogging))
            .And(x => GivenOptions())
            .And(x => _ws.ConnectAsync(ocelot, CancelMe))
            //await WhenISendAndReceiveEchoMessage();
            .When(x => _ws.SendAsync(upload, WebSocketMessageType.Text, true, CancelMe))
            .Then(x => ThenIReceiveAsync(body))
            .When(x => WhenITryToReproduceBug930())
            .And(x => Task.Delay(1_000))
            .Then(x => ThenEnsureTheBug930HasNotBeenReproducedViaLogger())
        .BDDfy();
    }
    private async Task ThenIReceiveAsync(string expected)
    {
        var echo = new byte[1024];
        var result = await _ws.ReceiveAsync(echo, CancelMe);
        result.CloseStatus.ShouldBeNull();
        result.Count.ShouldBe(expected.Length);
        Encoding.ASCII.GetString(echo).ShouldStartWith(expected);
    }
    private async Task WhenITryToReproduceBug930()
    {
        var exc = await Assert.ThrowsAsync<TaskCanceledException>(() =>
        {
            _cts.Cancel();
            return _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, Expected() + " has been sent", CancelMe);
        });
        _ws.Abort(); // !!! after cancellation of operations, let the connection be disposed aka finalized
    }

    public const string Bug930RootCause = "The WebSocket is in an invalid state ('Aborted') for this operation. Valid states are: 'Open, CloseReceived'";
    public const string Bugfix930ExpectedMessage = "WebSocketException when WebSocketErrorCode is ConnectionClosedPrematurely";
    private static void Bug930_StepsToReproduce(MemoryLogger logger)
    {
        logger.Exceptions.ShouldNotBeEmpty();
        string PrintExceptions() => string.Join(Environment.NewLine,
            logger.Exceptions.Select(e => $"{e.GetType().Name}: {e.Message}"));
        logger.Exceptions.ShouldContain(e => e.GetType() == typeof(WebSocketException), PrintExceptions());
        logger.Exceptions.Count(e => e.GetType() == typeof(WebSocketException)).ShouldBe(1, PrintExceptions());
        logger.Exceptions.Count.ShouldBe(1, PrintExceptions());
        var ex = logger.Exceptions.First();
        ex.ShouldBeOfType<WebSocketException>();
        ex.Message.ShouldBe(Bug930RootCause);
        logger.Messages.ShouldContain(m => m.Contains(Bug930RootCause));
        logger.Logbook.Contains(Bug930RootCause).ShouldBeTrue();
    }
    private void ThenEnsureTheBug930HasNotBeenReproducedViaLogger()
    {
        var factory = ocelotHost.Services.GetService<IOcelotLoggerFactory>();
        var logger = (factory as TestLoggerFactory<ClientWebSocketTests>).Logger;
        Assert.NotNull(logger);
        logger.Messages.ShouldNotBeEmpty();

        // STEPS TO REPRODUCE with old code, based on commit: https://github.com/ThreeMammals/Ocelot/commit/0b794b39e26d8bb538006eb5834b841c893c6611
        // Bug930_StepsToReproduce(logger);
        logger.Exceptions.ShouldBeEmpty(); // no errors on Ocelot's side, as they were swallowed in favor of logging a warning
        logger.Messages.ShouldNotContain(m => m.Contains(Bug930RootCause)); // no bug
        logger.Logbook.Contains(Bug930RootCause).ShouldBeFalse(); // no bug in the log
        try
        {
            logger.Messages.ShouldContain(m => m.Contains(Bugfix930ExpectedMessage)); // logged warning
            logger.Logbook.Contains(Bugfix930ExpectedMessage).ShouldBeTrue(); // logged warning
        }
        catch
        {
            // Be tolerant of attempted assertions, as they sometimes fail when the 'ConnectionClosedPrematurely' exception is not generated, thus the logbook is empty
        }
    }

    private static string Expected([CallerMemberName] string testName = null)
        => testName ?? nameof(ClientWebSocketTests);

    private static FileRoute GivenWsRoute(string scheme, string host, int port, string downstreamPath = null) => new()
    {
        UpstreamPathTemplate = "/",
        DownstreamPathTemplate = downstreamPath ?? "/",
        DownstreamScheme = scheme ?? Uri.UriSchemeWs,
        DownstreamHostAndPorts = [ new(host, port) ],
    };

    private void GivenOptions()
    {
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
    }

    private void GivenHttp2Options()
    {
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
    }

    private async Task WhenISendAndReceiveEchoMessage([CallerMemberName] string message = null)
    {
        var upload = Encoding.UTF8.GetBytes(message);
        await _ws.SendAsync(upload, WebSocketMessageType.Text, true, CancelMe);
        var echo = new byte[1024];
        var result = await _ws.ReceiveAsync(echo, CancelMe);
        string actual = Encoding.UTF8.GetString(echo, 0, result.Count);
        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, message + " has been sent", CancelMe);
        _echo = actual;
    }

    private void ThenEchoShouldBe(string expected) => _echo.ShouldBe(expected);
    private void ThenEchoShouldStartWith(string start) => _echo.ShouldStartWith(start);
}
