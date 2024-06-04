using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Kubernetes.Interfaces;

namespace Ocelot.Provider.Kubernetes;

public static class OcelotBuilderExtensions
{
    public static IOcelotBuilder AddKubernetes(this IOcelotBuilder builder, bool usePodServiceAccount = true)
    {
        builder.Services
            .AddKubeClient(usePodServiceAccount)
            .AddSingleton(KubernetesProviderFactory.Get)
            .AddSingleton<IKubeServiceBuilder, KubeServiceBuilder>()
            .AddSingleton<IKubeServiceCreator, KubeServiceCreator>();
        return builder;
    }
}
