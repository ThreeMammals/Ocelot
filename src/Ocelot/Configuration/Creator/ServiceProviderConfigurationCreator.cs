using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class ServiceProviderConfigurationCreator : IServiceProviderConfigurationCreator
    {
        public ServiceProviderConfiguration Create(FileGlobalConfiguration globalConfiguration)
        {
            var serviceProviderPort = globalConfiguration?.ServiceDiscoveryProvider?.Port ?? 0;

            return new ServiceProviderConfigurationBuilder()
                    .WithServiceDiscoveryProviderHost(globalConfiguration?.ServiceDiscoveryProvider?.Host)
                    .WithServiceDiscoveryProviderPort(serviceProviderPort)
                    .Build();
        }
    }
}