using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.Builder;
using Ocelot.Provider.Eureka;
using Steeltoe.Discovery;

namespace Ocelot.UnitTests.Eureka;

public class EurekaProviderFactoryTests
{
    [Fact]
    public void Should_not_get()
    {
        // Arrange
        var config = new ServiceProviderConfigurationBuilder().Build();
        var sp = new ServiceCollection().BuildServiceProvider(true);

        // Act, Assert
        Should.Throw<NullReferenceException>(() => EurekaProviderFactory.Get(sp, config, null));
    }

    [Fact]
    public void Should_get()
    {
        // Arrange
        var config = new ServiceProviderConfigurationBuilder().WithType("eureka").Build();
        var client = new Mock<IDiscoveryClient>();
        var services = new ServiceCollection();
        services.AddSingleton(client.Object);
        var sp = services.BuildServiceProvider(true);
        var route = new DownstreamRouteBuilder()
            .WithServiceName(string.Empty)
            .Build();

        // Act
        var provider = EurekaProviderFactory.Get(sp, config, route);

        // Assert
        provider.ShouldBeOfType<Provider.Eureka.Eureka>();
    }
}
