using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Eureka;
using Ocelot.ServiceDiscovery;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Http.Discovery;
using System.Reflection;

namespace Ocelot.UnitTests.Eureka;

public sealed class OcelotBuilderExtensionsTests : UnitTest
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configRoot;
    private IOcelotBuilder _ocelotBuilder;

    public OcelotBuilderExtensionsTests()
    {
        _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
        _services = new ServiceCollection();
        _services.AddSingleton(GetHostingEnvironment());
        _services.AddSingleton(_configRoot);
    }

    private static IWebHostEnvironment GetHostingEnvironment()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.ApplicationName)
            .Returns(typeof(OcelotBuilderExtensionsTests).GetTypeInfo().Assembly.GetName().Name);
        return environment.Object;
    }

    [Fact]
    [Trait("PR", "734")]
    [Trait("Feat", "324")]
    [Trait("Feat", "844")]
    public void AddEureka_NoExceptions_ShouldSetUpEureka()
    {
        // Arrange
        var addOcelot = () => _ocelotBuilder = _services.AddOcelot(_configRoot);
        addOcelot.ShouldNotThrow();

        // Act
        var addEureka = () => _ocelotBuilder.AddEureka();

        // Assert
        addEureka.ShouldNotThrow();
    }

    [Fact]
    [Trait("PR", "734")]
    [Trait("Feat", "324")]
    [Trait("Feat", "844")]
    public void AddEureka_DefaultServices_HappyPath()
    {
        // Arrange, Act
        _ocelotBuilder = _services.AddOcelot(_configRoot).AddEureka();

        // Assert: AddDiscoveryClient
        var descriptor = _services.SingleOrDefault(Of<DiscoveryHttpMessageHandler>).ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
        descriptor = _services.SingleOrDefault(Of<IServiceInstanceProvider>).ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);

        // Assert
        descriptor = _services.SingleOrDefault(Of<ServiceDiscoveryFinderDelegate>).ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor = _services.SingleOrDefault(Of<OcelotMiddlewareConfigurationDelegate>).ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    private static bool Of<TType>(ServiceDescriptor descriptor)
        where TType : class
        => descriptor.ServiceType.Equals(typeof(TType));
}
