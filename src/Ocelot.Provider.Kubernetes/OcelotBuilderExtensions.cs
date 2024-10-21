using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Kubernetes.Interfaces;

namespace Ocelot.Provider.Kubernetes;

public static class OcelotBuilderExtensions
{
    public static IOcelotBuilder AddKubernetes(this IOcelotBuilder builder, bool usePodServiceAccount = true)
    {
        builder.Services
            .AddSingleton<IKubeApiClient, KubeApiClient>(KubeApiClientFactory)
            .AddSingleton(KubernetesProviderFactory.Get)
            .AddSingleton<IKubeServiceBuilder, KubeServiceBuilder>()
            .AddSingleton<IKubeServiceCreator, KubeServiceCreator>();
        return builder;

        KubeApiClient KubeApiClientFactory(IServiceProvider sp)
        {
            if (usePodServiceAccount)
            {
                return KubeApiClient.CreateFromPodServiceAccount(sp.GetService<ILoggerFactory>());
            }

            KubeClientOptions options = sp.GetRequiredService<IOptions<KubeClientOptions>>().Value;
            options.LoggerFactory ??= sp.GetService<ILoggerFactory>();

            return KubeApiClient.Create(options);
        }
    }
}
