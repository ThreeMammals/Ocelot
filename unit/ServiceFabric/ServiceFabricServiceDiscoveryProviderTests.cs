using Ocelot.ServiceFabric;

namespace Ocelot.UnitTests.ServiceFabric;

public class ServiceFabricServiceDiscoveryProviderTests : UnitTest
{
    [Fact]
    public async Task Should_return_service_fabric_naming_service()
    {
        // Arrange
        const string host = "localhost";
        const int port = 19081;
        const string serviceName = "OcelotServiceApplication/OcelotApplicationService";

        // Act
        var config = new ServiceFabricConfiguration(host, port, serviceName);
        var provider = new ServiceFabricServiceDiscoveryProvider(config);
        var services = await provider.GetAsync();

        // Assert: Then The ServiceFabric Naming Service Is Retured
        provider.Name.ShouldBe(ServiceFabricServiceDiscoveryProvider.Type);
        services.Count.ShouldBe(1);
        services[0].Name.ShouldBe(serviceName);
        services[0].HostAndPort.DownstreamHost.ShouldBe(host);
        services[0].HostAndPort.DownstreamPort.ShouldBe(port);
    }
}
