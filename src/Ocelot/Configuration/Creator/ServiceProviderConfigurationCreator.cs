using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class ServiceProviderConfigurationCreator : IServiceProviderConfigurationCreator
    {
        public ServiceProviderConfiguration Create(FileGlobalConfiguration globalConfiguration)
        {
            var port = globalConfiguration?.ServiceDiscoveryProvider?.Port ?? 0;
            var host = globalConfiguration?.ServiceDiscoveryProvider?.Host ?? "localhost";
            var type = !string.IsNullOrEmpty(globalConfiguration?.ServiceDiscoveryProvider?.Type) 
                ? globalConfiguration?.ServiceDiscoveryProvider?.Type 
                : "consul";
            var pollingInterval = globalConfiguration?.ServiceDiscoveryProvider?.PollingInterval ?? 0;

            return new ServiceProviderConfigurationBuilder()
                .WithHost(host)
                .WithPort(port)
                .WithType(type)
                .WithToken(globalConfiguration?.ServiceDiscoveryProvider?.Token)
                .WithConfigurationKey(globalConfiguration?.ServiceDiscoveryProvider?.ConfigurationKey)
                .WithPollingInterval(pollingInterval)
                .Build();
        }
    }
}
