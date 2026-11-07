using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.Builder;
using Ocelot.DependencyInjection;
using Ocelot.DownstreamUrlCreator;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceFabric;

namespace Ocelot.UnitTests.ServiceFabric;

public class ServiceFabricServiceCollectionExtensionsTests : UnitTest
{
    [Fact]
    public async Task AddServiceFabric_should_register_formatter_and_matching_provider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDownstreamPathPlaceholderReplacer, DownstreamPathPlaceholderReplacer>();
        var builder = new Mock<IOcelotBuilder>();
        builder.Setup(x => x.Services).Returns(services);

        // Act
        var result = builder.Object.AddServiceFabric();
        using var provider = services.BuildServiceProvider();
        var finder = provider.GetRequiredService<ServiceDiscoveryFinderDelegate>();
        var discoveryProvider = finder(
            provider,
            new ServiceProviderConfigurationBuilder()
                .WithType("servicefabric")
                .WithHost("localhost")
                .WithPort(19081)
                .Build(),
            new DownstreamRouteBuilder()
                .WithServiceName("App/Service")
                .Build());
        var servicesFound = await discoveryProvider.GetAsync();

        // Assert
        result.ShouldBe(builder.Object);
        provider.GetRequiredService<IDownstreamPathFormatter>()
            .ShouldBeOfType<ServiceFabricDownstreamPathFormatter>();
        discoveryProvider.ShouldBeOfType<ServiceFabricServiceDiscoveryProvider>();
        servicesFound.ShouldHaveSingleItem();
        servicesFound[0].Name.ShouldBe("App/Service");
        servicesFound[0].HostAndPort.DownstreamHost.ShouldBe("localhost");
        servicesFound[0].HostAndPort.DownstreamPort.ShouldBe(19081);
    }

    [Fact]
    public void AddServiceFabric_should_return_null_for_a_different_provider_type()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Mock<IOcelotBuilder>();
        builder.Setup(x => x.Services).Returns(services);
        builder.Object.AddServiceFabric();
        using var provider = services.BuildServiceProvider();
        var finder = provider.GetRequiredService<ServiceDiscoveryFinderDelegate>();

        // Act
        var result = finder(
            provider,
            new ServiceProviderConfigurationBuilder()
                .WithType("Consul")
                .Build(),
            new DownstreamRouteBuilder()
                .WithServiceName("App/Service")
                .Build());

        // Assert
        result.ShouldBeNull();
    }
}
