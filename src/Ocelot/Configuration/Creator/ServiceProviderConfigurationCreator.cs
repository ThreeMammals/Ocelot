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
                .WithServiceDiscoveryProviderHost(globalConfiguration?.ServiceDiscoveryProvider?.Host)
                .WithServiceDiscoveryProviderPort(serviceProviderPort)
                .WithServiceDiscoveryProviderType(globalConfiguration?.ServiceDiscoveryProvider?.Type)
                .Build();
        }
    }
}
