using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Testing.Steps;

namespace Ocelot.AcceptanceTests.WebSockets;

[Trait("Feat", "212")] // https://github.com/ThreeMammals/Ocelot/issues/212
[Trait("PR", "273")] // https://github.com/ThreeMammals/Ocelot/pull/273
public sealed class WebSocketsFactoryTests : WebSocketsSteps
{
    [BddfyFact]
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
            .And(_ => GivenWebSocketsServiceIsRunningAsync(port, "/ws", EchoAsync, CancelMe))
            .When(_ => StartClient(ocelotUrl))
            .Then(_ => ThenTheReceivedCountIs(10))
        .BDDfy();
    }
    private void ThenTheReceivedCountIs(int count) => _firstRecieved.Count.ShouldBe(count);

    [BddfyFact]
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
            .And(_ => GivenWebSocketsServiceIsRunningAsync(port1, "/ws", EchoAsync, CancelMe))
            .And(_ => GivenWebSocketsServiceIsRunningAsync(port2, "/ws", MessageAsync, CancelMe))
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
