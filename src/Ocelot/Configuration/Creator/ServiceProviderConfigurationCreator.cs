using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class ServiceProviderConfigurationCreator : IServiceProviderConfigurationCreator
    {
        public ServiceProviderConfiguration Create(FileGlobalConfiguration globalConfiguration)
        {
            //todo log or return error here dont just default to something that wont work..
            var serviceProviderPort = globalConfiguration?.ServiceDiscoveryProvider?.Port ?? 0;

            return new ServiceProviderConfigurationBuilder()
                .WithHost(globalConfiguration?.ServiceDiscoveryProvider?.Host)
                .WithPort(serviceProviderPort)
                .WithType(globalConfiguration?.ServiceDiscoveryProvider?.Type)
                .WithToken(globalConfiguration?.ServiceDiscoveryProvider?.Token)
                .WithConfigurationKey(globalConfiguration?.ServiceDiscoveryProvider?.ConfigurationKey)
                .Build();
        }
    }
}
