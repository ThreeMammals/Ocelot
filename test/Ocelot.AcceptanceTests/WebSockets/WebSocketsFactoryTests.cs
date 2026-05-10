using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.WebSockets;

namespace Ocelot.AcceptanceTests.WebSockets;

public sealed class WebSocketsFactoryTests : WebSocketsSteps
{
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
        await StartOcelotWithWebSockets(ocelotPort, null);
        await GivenWebSocketsServiceIsRunningAsync(port, "/ws", EchoAsync, CancellationToken.None);
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
        route.LoadBalancerOptions = new(nameof(RoundRobin));
        var configuration = GivenConfiguration(route);
        int ocelotPort = PortFinder.GetRandomPort();
        this.Given(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => StartOcelotWithWebSockets(ocelotPort, null))
            .And(_ => GivenWebSocketsServiceIsRunningAsync(port1, "/ws", EchoAsync, CancellationToken.None))
            .And(_ => GivenWebSocketsServiceIsRunningAsync(port2, "/ws", MessageAsync, CancellationToken.None))
            .When(_ => WhenIStartTheClients(ocelotPort))
            .Then(_ => ThenBothDownstreamServicesAreCalled())
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "2386")]
    [Trait("PR", "2387")]
    public async Task ShouldProxyWebsocketInputToDownstreamServiceUsingCustomMiddleware()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute("/ws", port);
        GivenThereIsAConfiguration(GivenConfiguration(route));
        int ocelotPort = PortFinder.GetRandomPort();
        bool customMiddlewareInvoked = false;
        var pipelineConfig = new OcelotPipelineConfiguration
        {
            WebSocketsProxyMiddleware = (context, next) =>
            {
                customMiddlewareInvoked = true;
                var loggerFactory = context.RequestServices.GetRequiredService<IOcelotLoggerFactory>();
                var factory = context.RequestServices.GetRequiredService<IWebSocketsFactory>();
                var middleware = new LargeBufferWebSocketsProxyMiddleware(loggerFactory, _ => next(), factory);
                return middleware.Invoke(context);
            },
        };
        await StartOcelotWithWebSockets(ocelotPort, null, pipelineConfig);
        await GivenWebSocketsServiceIsRunningAsync(port, "/ws", EchoAsync, CancellationToken.None);
        await StartClient(new UriBuilder(Uri.UriSchemeWs, "localhost", ocelotPort).Uri);
        customMiddlewareInvoked.ShouldBeTrue();
        _firstRecieved.Count.ShouldBe(10);
    }

    private FileRoute GivenRoute(string downstream = null, params int[] ports) => new()
    {
        UpstreamPathTemplate = "/",
        DownstreamPathTemplate = downstream ?? "/ws",
        DownstreamScheme = Uri.UriSchemeWs,
        DownstreamHostAndPorts = ports.Select(Localhost).ToList(),
    };

    private sealed class LargeBufferWebSocketsProxyMiddleware : WebSocketsProxyMiddleware
    {
        protected override int DefaultWebSocketBufferSize => 65536;

        public LargeBufferWebSocketsProxyMiddleware(IOcelotLoggerFactory logging, RequestDelegate next, IWebSocketsFactory factory)
            : base(logging, next, factory) { }
    }
}
