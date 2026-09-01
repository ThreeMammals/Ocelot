using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Testing.Steps;
using Ocelot.WebSockets;
using System.Net.WebSockets;
using System.Text;

namespace Ocelot.Acceptance.WebSockets;

[Trait("Feat", "212")] // https://github.com/ThreeMammals/Ocelot/issues/212
public sealed class WebSocketsFactoryTests : WebSocketsSteps
{
    [Fact]
    [Trait("PR", "273")] // https://github.com/ThreeMammals/Ocelot/pull/273
    public void ShouldProxyWebsocketInputToDownstreamService()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute("/ws", port);
        var configuration = GivenConfiguration(route);
        int ocelotPort = PortFinder.GetRandomPort();
        var ocelotUrl = new UriBuilder(Uri.UriSchemeWs, "localhost", ocelotPort).Uri;
        this
            .Given(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => StartOcelotWithWebSockets(ocelotPort, null))
            .And(_ => GivenWebSocketsServiceIsRunningAsync(port, "/ws", EchoAsync))
            .When(_ => StartClient(ocelotUrl, CancelMe))
            .Then(_ => ThenTheReceivedCountIs(10))
        .BDDfy();
    }
    private void ThenTheReceivedCountIs(int count) => _firstRecieved.Count.ShouldBe(count);

    [Fact]
    [Trait("PR", "273")] // https://github.com/ThreeMammals/Ocelot/pull/273
    public void ShouldProxyWebsocketInputToDownstreamServiceAndUseLoadBalancer()
    {
        int port1 = PortFinder.GetRandomPort();
        int port2 = PortFinder.GetRandomPort();
        var route = GivenRoute("/ws", port1, port2);
        route.LoadBalancerOptions = new(nameof(RoundRobin));
        var configuration = GivenConfiguration(route);
        int ocelotPort = PortFinder.GetRandomPort();
        this
            .Given(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => StartOcelotWithWebSockets(ocelotPort, null))
            .And(_ => GivenWebSocketsServiceIsRunningAsync(port1, "/ws", EchoAsync))
            .And(_ => GivenWebSocketsServiceIsRunningAsync(port2, "/ws", MessageAsync))
            .When(_ => WhenIStartTheClients(ocelotPort))
            .Then(_ => ThenBothDownstreamServicesAreCalled())
        .BDDfy();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Feat", "2386")] // https://github.com/ThreeMammals/Ocelot/issues/2386
    [Trait("PR", "2387")] // https://github.com/ThreeMammals/Ocelot/pull/2387
    public async Task ShouldProxyWebsocketInputToDownstreamServiceUsingCustomMiddleware(bool injectViaType)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute("/ws", port);
        var configuration = GivenConfiguration(route);
        int ocelotPort = PortFinder.GetRandomPort();
        var ocelotUrl = new UriBuilder(Uri.UriSchemeWs, "localhost", ocelotPort).Uri;
        bool customMiddlewareInvoked = false;
        Task CreateMiddleware(HttpContext context, Func<Task> next)
        {
            customMiddlewareInvoked = true;
            var loggerFactory = context.RequestServices.GetRequiredService<IOcelotLoggerFactory>();
            var factory = context.RequestServices.GetRequiredService<IWebSocketsFactory>();
            var middleware = new LargeBufferWebSocketsProxyMiddleware(_ => next(), loggerFactory, factory);
            return middleware.Invoke(context);
        }
        var pipelineConfig = new OcelotPipelineConfiguration
        {
            WebSocketsMiddlewareType = injectViaType ? typeof(LargeBufferWebSocketsProxyMiddleware) : null,
            WebSocketsMiddleware = injectViaType ? null : CreateMiddleware,
        };
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => StartOcelotWithWebSockets(ocelotPort, null, pipelineConfig))
            .And(x => GivenWebSocketsServiceIsRunningAsync(port, "/ws", EchoAsync))
            .When(x => StartClient(ocelotUrl, CancelMe))
            .Then(x => ThenTheReceivedCountIs(10))
            .And(x => customMiddlewareInvoked.ShouldBe(!injectViaType, null))
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "2386")] // https://github.com/ThreeMammals/Ocelot/issues/2386
    [Trait("PR", "2390")] // https://github.com/ThreeMammals/Ocelot/pull/2390
    public async Task ShouldProxyWebSocketWithConfiguredBufferSize()
    {
        int port = PortFinder.GetRandomPort();
        var route = GivenRoute("/ws", port);
        route.WebSocket = new FileWebSocketOptions { BufferSize = 65536 }; // 64 KB — overrides the 4096 default
        var configuration = GivenConfiguration(route);
        int ocelotPort = PortFinder.GetRandomPort();
        var ocelotUrl = new UriBuilder(Uri.UriSchemeWs, "localhost", ocelotPort).Uri;
        this
            .Given(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => StartOcelotWithWebSockets(ocelotPort, null))
            .And(_ => GivenWebSocketsServiceIsRunningAsync(port, "/ws", EchoLargeAsync))
            .When(_ => WhenIConnectAndSendALargePayload(ocelotUrl))
            .Then(_ => ThenTheLargePayloadIsEchoedBack())
        .BDDfy();
    }

    private string _largePayload;
    private string _largePayloadReceived;

    private async Task WhenIConnectAndSendALargePayload(Uri ocelotUrl)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var client = new ClientWebSocket();
        await client.ConnectAsync(ocelotUrl, cts.Token);

        // 32 KB payload — larger than the 4096-byte default buffer
        _largePayload = new string('A', 1024 * 32);
        var upload = Encoding.UTF8.GetBytes(_largePayload);
        await client.SendAsync(new ArraySegment<byte>(upload), WebSocketMessageType.Text, true, cts.Token);

        var downloadBuffer = new byte[1024 * 64];
        var result = await client.ReceiveAsync(new ArraySegment<byte>(downloadBuffer), cts.Token);
        _largePayloadReceived = Encoding.UTF8.GetString(downloadBuffer, 0, result.Count);
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cts.Token);
    }

    private void ThenTheLargePayloadIsEchoedBack() => _largePayloadReceived.ShouldBe(_largePayload);

    private FileRoute GivenRoute(string downstream = null, params int[] ports) => new()
    {
        UpstreamPathTemplate = "/",
        DownstreamPathTemplate = downstream ?? "/ws",
        DownstreamScheme = Uri.UriSchemeWs,
        DownstreamHostAndPorts = ports.Select(Localhost).ToList(),
    };

    public override CancellationToken CancelMe => Xunit.TestContext.Current.CancellationToken;

    private sealed class LargeBufferWebSocketsProxyMiddleware : WebSocketsProxyMiddleware
    {
        protected override int BufferSize => 65536;
        public LargeBufferWebSocketsProxyMiddleware(RequestDelegate next, IOcelotLoggerFactory logging, IWebSocketsFactory factory)
            : base(next, logging, factory) { }
    }
}
