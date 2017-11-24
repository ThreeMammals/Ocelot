using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Repository;

namespace Ocelot.DependencyInjection
{
    public static class OcelotBuilderExtensionsConsul
    {
        public static IOcelotBuilder AddStoreOcelotConfigurationInConsul(this IOcelotBuilder builder)
        {
            var configuration = builder.Configuration;

            var serviceDiscoveryPort = configuration.GetValue("GlobalConfiguration:ServiceDiscoveryProvider:Port", 0);
            var serviceDiscoveryHost = configuration.GetValue("GlobalConfiguration:ServiceDiscoveryProvider:Host", string.Empty);

            var config = new ServiceProviderConfigurationBuilder()
                .WithServiceDiscoveryProviderPort(serviceDiscoveryPort)
                .WithServiceDiscoveryProviderHost(serviceDiscoveryHost)
                .Build();

            var services = builder.Services;

            services.AddSingleton<ServiceProviderConfiguration>(config);
            services.AddSingleton<ConsulFileConfigurationPoller>();
            services.AddSingleton<IFileConfigurationRepository, ConsulFileConfigurationRepository>();

            return builder;
        }
    }
}
