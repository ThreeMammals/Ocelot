using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;

namespace Ocelot.Provider.Kubernetes
{
    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddKubernetes(this IOcelotBuilder builder, bool usePodServiceAccount = true)
        {
            builder.Services
                .AddSingleton(KubernetesProviderFactory.Get)
                .AddKubeClient(usePodServiceAccount);
            return builder;
        }
    }
}
