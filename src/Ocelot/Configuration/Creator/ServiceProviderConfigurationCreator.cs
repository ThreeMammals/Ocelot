using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class ServiceProviderConfigurationCreator : IServiceProviderConfigurationCreator
    {
        public ServiceProviderConfiguration Create(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var useServiceDiscovery = !string.IsNullOrEmpty(fileReRoute.ServiceName)
                && !string.IsNullOrEmpty(globalConfiguration?.ServiceDiscoveryProvider?.Provider);

            var serviceProviderPort = globalConfiguration?.ServiceDiscoveryProvider?.Port ?? 0;

            return new ServiceProviderConfigurationBuilder()
                    .WithServiceName(fileReRoute.ServiceName)
                    .WithDownstreamHost(fileReRoute.DownstreamHost)
                    .WithDownstreamPort(fileReRoute.DownstreamPort)
                    .WithUseServiceDiscovery(useServiceDiscovery)
                    .WithServiceDiscoveryProvider(globalConfiguration?.ServiceDiscoveryProvider?.Provider)
                    .WithServiceDiscoveryProviderHost(globalConfiguration?.ServiceDiscoveryProvider?.Host)
                    .WithServiceDiscoveryProviderPort(serviceProviderPort)
                    .Build();
        }
    }
}