using KubeClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Kubernetes;
using Ocelot.ServiceDiscovery;

namespace Ocelot.UnitTests.Kubernetes;

public class KubernetesProviderFactoryTests : UnitTest
{
    private readonly ServiceCollection _services = new();
    private IOcelotBuilder _builder;
    private ServiceProvider _serviceProvider;

    [Theory]
    [Trait("Bug", "977")]
    [InlineData(nameof(PollKube))]
    [InlineData(nameof(Kube))]
    public void Should_resolve_Provider(string providerType)
    {
        this.Given(s => s.GivenOcelotBuilder())
            .When(s => s.WhenKubernetesRegistered())
            .When(s => s.WhenServiceProviderBuilt(true))
            .Then(s => s.ThenKubeDiscoveryProviderCanBeResolved(providerType))
            .BDDfy();
    }

    private void GivenOcelotBuilder()
    {
        _builder = _services.AddOcelot();
    }

    private void WhenKubernetesRegistered()
    {
        _builder.AddKubernetes();

        // mocked IKubeApiClient should have the same lifetime
        var kubeApiClientDescriptor = _services.First(x => x.ServiceType == typeof(IKubeApiClient));
        var sd = new ServiceDescriptor(kubeApiClientDescriptor.ServiceType,
            _ => Mock.Of<IKubeApiClient>(),
            kubeApiClientDescriptor.Lifetime);
        _builder.Services.Replace(sd);
    }

    private void WhenServiceProviderBuilt(bool validateScopes)
    {
        _serviceProvider = _services.BuildServiceProvider(validateScopes);
    }

    private void ThenKubeDiscoveryProviderCanBeResolved(string providerType)
    {
        var resolving = () =>
        {
            var finder = _serviceProvider.GetRequiredService<ServiceDiscoveryFinderDelegate>();

            var config = new ServiceProviderConfiguration(providerType,
                scheme: string.Empty,
                host: string.Empty,
                port: 1,
                token: string.Empty,
                configurationKey: string.Empty,
                pollingInterval: 1);

            var route = new DownstreamRouteBuilder()
                .WithServiceName("service")
                .Build();

            _ = finder.Invoke(_serviceProvider, config, route);
        };
        
        resolving.ShouldNotThrow();
    }
}
