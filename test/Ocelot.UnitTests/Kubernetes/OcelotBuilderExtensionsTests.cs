using KubeClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        AssertServices();
    }

    [Fact]
    [Trait("Feat", "2256")]
    public void AddKubernetes_NoAction_HappyPath()
    {
        // Arrange
        Action<KubeClientOptions> noAction = null;
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddKubernetes(noAction);

        // Assert
        AssertServices();
        Assert<IConfigureOptions<KubeClientOptions>>(); // not IOptions<KubeClientOptions>
    }

    private void AssertServices()
    {
        Assert<IKubeApiClient>(); // 2180 scenario
        Assert<ServiceDiscoveryFinderDelegate>();
        Assert<IKubeServiceBuilder>();
        Assert<IKubeServiceCreator>();
    }

    private void Assert<T>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where T : class
    {
        var descriptor = _services.SingleOrDefault(Of<T>).ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(lifetime);
    }

    private static bool Of<T>(ServiceDescriptor descriptor)
        where T : class
        => descriptor.ServiceType.Equals(typeof(T));
}
