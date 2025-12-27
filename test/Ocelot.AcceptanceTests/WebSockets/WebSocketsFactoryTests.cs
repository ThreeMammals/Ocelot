using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.Balancers;

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

    private FileRoute GivenRoute(string downstream = null, params int[] ports) => new()
    {
        UpstreamPathTemplate = "/",
        DownstreamPathTemplate = downstream ?? "/ws",
        DownstreamScheme = Uri.UriSchemeWs,
        DownstreamHostAndPorts = ports.Select(Localhost).ToList(),
    };
}
