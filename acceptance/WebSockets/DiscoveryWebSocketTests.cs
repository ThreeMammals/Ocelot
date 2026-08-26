using Microsoft.Extensions.DependencyInjection;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Testing.Steps;
using System.Net.WebSockets;

namespace Ocelot.Acceptance.WebSockets;

public sealed class DiscoveryWebSocketTests : WebSocketsSteps
{
    private readonly DiscoveryWebSocketSteps steps = new();
    public override void Dispose()
    {
        steps.Dispose();
        base.Dispose();
    }

    [Fact(DisplayName = "TODO " + nameof(Should_proxy_websocket_input_to_downstream_service_and_use_service_discovery_and_load_balancer))]
    [Trait("Feat", "212")] // https://github.com/ThreeMammals/Ocelot/issues/212
    [Trait("PR", "273")] // https://github.com/ThreeMammals/Ocelot/pull/273
    [Trait("Release", "5.3.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/5.3.0
    [Trait("Commit", "463a7bd")] // https://github.com/ThreeMammals/Ocelot/commit/463a7bdab4652762d14779e7e3f62a207c3d421c
    public void Should_proxy_websocket_input_to_downstream_service_and_use_service_discovery_and_load_balancer()
    {
        var ports = PortFinder.GetPorts(2);
        const string serviceName = "websockets"; // steps.ServiceName();
        steps.InitializeCounters(ports.Length);

        var route = GivenRoute(0, "/", "/ws");
        route.DownstreamHostAndPorts.Clear(); // read hosts via service discovery
        route.DownstreamScheme = Uri.UriSchemeWs;
        route.LoadBalancerOptions = new(nameof(RoundRobin)); // TODO Add more load balancers and convert to Theory
        route.ServiceName = serviceName;
        steps.GivenDiscoveryMetadata(route, ports);

        var configuration = steps.GivenStaticRouting(null, route);
        int ocelotPort = PortFinder.GetRandomPort();
        this.Given(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => StartOcelotWithWebSockets(ocelotPort, WithDiscovery))
            .And(_ => GivenWebSocketsServiceIsRunningAsync(ports[0], "/ws", EchoAsync)) // test -> test -> test -> ...
            .And(_ => GivenWebSocketsServiceIsRunningAsync(ports[1], "/ws", MessageAsync)) // chocolate -> chocolate -> chocolate -> ...
            .When(_ => WhenIStartTheClients(ocelotPort)) // getting to Ocelot on "/" upstream. It is cumbersome, but runs in parallel
            .Then(_ => ThenBothDownstreamServicesAreCalled())
            .And(_ => ThenServicesShouldHaveBeenCalledTimes(1, 1)) // RoundRobin algorithm
        .BDDfy();
    }

    private void WithDiscovery(IServiceCollection services)
        => steps.WithDiscovery(services);
    private void ThenServicesShouldHaveBeenCalledTimes(params int[] expected)
        => steps.ThenServicesShouldHaveBeenCalledTimes(expected);

    protected override async Task EchoAsync(WebSocket webSocket, CancellationToken token)
    {
        await base.EchoAsync(webSocket, token);
        // Then connection is closed, thus...
        steps.IncrementCounter(0); // stick to the port with index at 0
    }
    protected override async Task MessageAsync(WebSocket webSocket, CancellationToken token)
    {
        await base.MessageAsync(webSocket, token);
        // Then connection is closed, thus...
        steps.IncrementCounter(1); // stick to the port with index at 1
    }
}

public class DiscoveryWebSocketSteps : DiscoverySteps
{
    public void InitializeCounters(int size)
        => Counters = new int[size];
    public void IncrementCounter(int index)
        => Interlocked.Increment(ref Counters[index]);
}
