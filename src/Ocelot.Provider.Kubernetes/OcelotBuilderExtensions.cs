using KubeClient;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.ServiceDiscovery;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Provider.Kubernetes
{
    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddKubernetes(this IOcelotBuilder builder)
        {
            builder.Services.AddSingleton(KubernetesProviderFactory.Get);
            builder.Services.AddSingleton<IKubeApiClientFactory, KubeApiClientFactory>();
            return builder;
        }
    }
}
