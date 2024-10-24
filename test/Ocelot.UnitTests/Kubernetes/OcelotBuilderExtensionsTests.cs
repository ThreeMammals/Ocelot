using KubeClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.ServiceDiscovery;
using System.Reflection;

namespace Ocelot.UnitTests.Kubernetes;

public class OcelotBuilderExtensionsTests : UnitTest // No Chinese tests now!
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
    [Trait("Feat", "345")]
    public void AddKubernetes_NoExceptions_ShouldSetUpKubernetes()
    {
        // Arrange
        var addOcelot = () => _ocelotBuilder = _services.AddOcelot(_configRoot);
        addOcelot.ShouldNotThrow();

        // Act
        var addKubernetes = () => _ocelotBuilder.AddKubernetes();

        // Assert
        addKubernetes.ShouldNotThrow();
    }

    [Fact]
    [Trait("Bug", "977")]
    [Trait("PR", "2180")]
    public void AddKubernetes_DefaultServices_HappyPath()
    {
        // Arrange, Act
        _ocelotBuilder = _services.AddOcelot(_configRoot).AddKubernetes();

        // Assert
        var descriptor = _services.SingleOrDefault(sd => sd.ServiceType == typeof(IKubeApiClient)).ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton); // 2180 scenario

        descriptor = _services.SingleOrDefault(sd => sd.ServiceType == typeof(ServiceDiscoveryFinderDelegate)).ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);

        descriptor = _services.SingleOrDefault(sd => sd.ServiceType == typeof(IKubeServiceBuilder)).ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);

        descriptor = _services.SingleOrDefault(sd => sd.ServiceType == typeof(IKubeServiceCreator)).ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }
}
