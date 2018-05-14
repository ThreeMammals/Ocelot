using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class ServiceProviderConfigurationCreator : IServiceProviderConfigurationCreator
    {
        public ServiceProviderConfiguration Create(FileGlobalConfiguration globalConfiguration)
        {
            var port = globalConfiguration?.ServiceDiscoveryProvider?.Port ?? 0;
            var host = globalConfiguration?.ServiceDiscoveryProvider?.Host ?? "consul";

            return new ServiceProviderConfigurationBuilder()
                .WithHost(host)
                .WithPort(port)
                .WithType(globalConfiguration?.ServiceDiscoveryProvider?.Type)
                .WithToken(globalConfiguration?.ServiceDiscoveryProvider?.Token)
                .WithConfigurationKey(globalConfiguration?.ServiceDiscoveryProvider?.ConfigurationKey)
                .Build();
        }
    }
}
