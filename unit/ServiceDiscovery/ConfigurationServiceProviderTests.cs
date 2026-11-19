using Ocelot.ServiceDiscovery;
using Ocelot.Values;

namespace Ocelot.UnitTests.ServiceDiscovery;

public class ConfigurationServiceProviderTests : UnitTest
{
    private ConfigurationServiceProvider _serviceProvider;

    [Fact]
    public async Task Should_return_services()
    {
        // Arrange
        var hostAndPort = new ServiceHostAndPort("127.0.0.1", 80);
        var services = new List<Service>
        {
            new("product", hostAndPort, string.Empty, string.Empty, Array.Empty<string>()),
        };
        _serviceProvider = new ConfigurationServiceProvider(services);

        // Act
        var result = await _serviceProvider.GetAsync();

        // Assert
        _serviceProvider.Name.ShouldBe(nameof(ConfigurationServiceProvider));
        result[0].HostAndPort.DownstreamHost.ShouldBe(services[0].HostAndPort.DownstreamHost);
        result[0].HostAndPort.DownstreamPort.ShouldBe(services[0].HostAndPort.DownstreamPort);
        result[0].Name.ShouldBe(services[0].Name);
    }
}
