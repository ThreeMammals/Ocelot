namespace Ocelot.Provider.Eureka
{
    using Microsoft.Extensions.DependencyInjection;
    using ServiceDiscovery;
    using Steeltoe.Common.Discovery;

    public static class EurekaProviderFactory
    {
        public static ServiceDiscoveryFinderDelegate Get = (provider, config, reRoute) =>
        {
            var client = provider.GetService<IDiscoveryClient>();

            if (config.Type?.ToLower() == "eureka" && client != null)
            {
                return new Eureka(reRoute.ServiceName, client);
            }

            return null;
        };
    }
}
