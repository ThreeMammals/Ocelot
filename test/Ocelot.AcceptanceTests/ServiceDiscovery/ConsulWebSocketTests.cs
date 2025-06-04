using Consul;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.AcceptanceTests.WebSockets;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Consul;
using System.Text;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

public sealed class ConsulWebSocketTests : WebSocketsSteps
{
    private readonly List<ServiceEntry> _serviceEntries = new();

    [Fact]
    public void ShouldProxyWebsocketInputToDownstreamServiceAndUseServiceDiscoveryAndLoadBalancer()
    {
        var downstreamPort = PortFinder.GetRandomPort();
        var downstreamHost = "localhost";

        var secondDownstreamPort = PortFinder.GetRandomPort();
        var secondDownstreamHost = "localhost";

        var serviceName = "websockets";
        var consulPort = PortFinder.GetRandomPort();
        var serviceEntryOne = new ServiceEntry
        {
            Service = new AgentService
            {
                Service = serviceName,
                Address = downstreamHost,
                Port = downstreamPort,
                ID = Guid.NewGuid().ToString(),
                Tags = Array.Empty<string>(),
            },
        };
        var serviceEntryTwo = new ServiceEntry
        {
            Service = new AgentService
            {
                Service = serviceName,
                Address = secondDownstreamHost,
                Port = secondDownstreamPort,
                ID = Guid.NewGuid().ToString(),
                Tags = Array.Empty<string>(),
            },
        };

        var config = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    UpstreamPathTemplate = "/",
                    DownstreamPathTemplate = "/ws",
                    DownstreamScheme = "ws",
                    LoadBalancerOptions = new FileLoadBalancerOptions { Type = "RoundRobin" },
                    ServiceName = serviceName,
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = consulPort,
                    Type = "consul",
                },
            },
        };
        int ocelotPort = PortFinder.GetRandomPort();
        this.Given(_ => GivenThereIsAConfiguration(config))
            .And(_ => StartOcelotWithWebSockets(ocelotPort, WithConsul))
            .And(_ => GivenThereIsAFakeConsulServiceDiscoveryProvider(consulPort, serviceName))
            .And(_ => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
            .And(_ => GivenWebSocketsServiceIsRunningAsync(downstreamPort, "/ws", EchoAsync, CancellationToken.None))
            .And(_ => GivenWebSocketsServiceIsRunningAsync(secondDownstreamPort, "/ws", MessageAsync, CancellationToken.None))
            .When(_ => WhenIStartTheClients(ocelotPort))
            .Then(_ => ThenBothDownstreamServicesAreCalled())
            .BDDfy();
    }

    private void WithConsul(IServiceCollection services) => services.AddOcelot().AddConsul();

    private void GivenTheServicesAreRegisteredWithConsul(params ServiceEntry[] serviceEntries)
    {
        foreach (var serviceEntry in serviceEntries)
        {
            _serviceEntries.Add(serviceEntry);
        }
    }

    private void GivenThereIsAFakeConsulServiceDiscoveryProvider(int port, string serviceName)
    {
        Task MapServicePath(HttpContext context)
        {
            if (context.Request.Path.Value == $"/v1/health/service/{serviceName}")
            {
                var json = JsonConvert.SerializeObject(_serviceEntries);
                context.Response.Headers.Append("Content-Type", "application/json");
                return context.Response.WriteAsync(json);
            }
            return Task.CompletedTask;
        }
        handler.GivenThereIsAServiceRunningOn(port, MapServicePath);
    }
}
