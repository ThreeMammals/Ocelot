using KubeClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.Kubernetes;

public class KubernetesProviderFactoryTests : UnitTest
{
    private readonly IOcelotBuilder _builder;

    public KubernetesProviderFactoryTests()
    {
        _builder = new ServiceCollection()
            .AddOcelot().AddKubernetes();
    }

    [Theory]
    [Trait("Bug", "977")]
    [InlineData(typeof(Kube))]
    [InlineData(typeof(PollKube))]
    public void CreateProvider_IKubeApiClientHasOriginalLifetimeWithEnabledScopesValidation_ShouldResolveProvider(Type providerType)
    {
        // Arrange
        var kubeClient = new Mock<IKubeApiClient>();
        kubeClient.Setup(x => x.ResourceClient(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns(Mock.Of<IEndPointClient>());
        var descriptor = _builder.Services.First(x => x.ServiceType == typeof(IKubeApiClient));
        _builder.Services.Replace(ServiceDescriptor.Describe(descriptor.ServiceType, _ => kubeClient.Object, descriptor.Lifetime));
        var serviceProvider = _builder.Services.BuildServiceProvider(validateScopes: true);
        var config = GivenServiceProvider(providerType.Name);
        var route = GivenRoute();

        // Act
        IServiceDiscoveryProvider actual = null;
        var resolving = () => actual = serviceProvider
            .GetRequiredService<ServiceDiscoveryFinderDelegate>() // returns KubernetesProviderFactory.Get instance
            .Invoke(serviceProvider, config, route);

        // Assert
        resolving.ShouldNotThrow();
        actual.ShouldNotBeNull().ShouldBeOfType(providerType);
    }
    
    [Theory]
    [Trait("Bug", "977")]
    [InlineData(nameof(Kube))]
    [InlineData(nameof(PollKube))]
    public void CreateProvider_IKubeApiClientHasScopedLifetimeWithEnabledScopesValidation_ShouldFailToResolve(string providerType)
    {
        // Arrange
        var descriptor = ServiceDescriptor.Describe(typeof(IKubeApiClient), _ => Mock.Of<IKubeApiClient>(), ServiceLifetime.Scoped);
        _builder.Services.Replace(descriptor);
        var serviceProvider = _builder.Services.BuildServiceProvider(validateScopes: true);
        var config = GivenServiceProvider(providerType);
        var route = GivenRoute();

        // Act
        IServiceDiscoveryProvider actual = null;
        var resolving = () => actual = serviceProvider
            .GetRequiredService<ServiceDiscoveryFinderDelegate>() // returns KubernetesProviderFactory.Get instance
            .Invoke(serviceProvider, config, route);

        // Assert
        var ex = resolving.ShouldThrow<InvalidOperationException>();
        ex.Message.ShouldContain("Cannot resolve scoped service 'KubeClient.IKubeApiClient' from root provider");
        actual.ShouldBeNull();
    }

    private static ServiceProviderConfiguration GivenServiceProvider(string type) => new(
        type: type,
        scheme: string.Empty,
        host: string.Empty,
        port: 1,
        token: string.Empty,
        configurationKey: string.Empty,
        pollingInterval: 1);

    private static DownstreamRoute GivenRoute([CallerMemberName] string serviceName = "test-service")
        => new DownstreamRouteBuilder().WithServiceName(serviceName).Build();
}
