using KubeClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.ServiceDiscovery;

namespace Ocelot.UnitTests.Kubernetes;

public class KubernetesProviderFactoryTests : UnitTest
{
    private readonly IOcelotBuilder _builder;

    public KubernetesProviderFactoryTests()
    {
        _builder = new ServiceCollection().AddOcelot();
    }

    [Theory]
    [Trait("Bug", "977")]
    [InlineData(nameof(PollKube))]
    [InlineData(nameof(Kube))]
    public void Should_resolve_Provider(string providerType)
    {
        // Arrange
        _builder.AddKubernetes();

        var kubeClient = new Mock<IKubeApiClient>();
        kubeClient
            .Setup(x => x.ResourceClient(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns(Mock.Of<IEndPointClient>());
        var sd = _builder.Services.First(x => x.ServiceType == typeof(IKubeApiClient));
        _builder.Services.Replace(ServiceDescriptor.Describe(sd.ServiceType, _ => kubeClient.Object, sd.Lifetime));

        var serviceProvider = _builder.Services.BuildServiceProvider(validateScopes: true);

        var config = GivenServiceProvider(providerType);
        var route = GivenRoute("test-service");

        // Act
        var resolving = () => _ = serviceProvider
            .GetRequiredService<ServiceDiscoveryFinderDelegate>()
            .Invoke(serviceProvider, config, route);

        // Assert
        resolving.ShouldNotThrow();
    }

    private static ServiceProviderConfiguration GivenServiceProvider(string type) => new(
        type: type,
        scheme: string.Empty,
        host: string.Empty,
        port: 1,
        token: string.Empty,
        configurationKey: string.Empty,
        pollingInterval: 1);

    private static DownstreamRoute GivenRoute(string name) => new DownstreamRouteBuilder().WithServiceName(name).Build();
}
