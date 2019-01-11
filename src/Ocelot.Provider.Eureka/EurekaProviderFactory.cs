namespace Ocelot.Provider.Eureka
{
    using Microsoft.Extensions.DependencyInjection;
    using ServiceDiscovery;
    using ServiceDiscovery.Providers;
    using Steeltoe.Common.Discovery;

    public static class EurekaProviderFactory
    {
        public static ServiceDiscoveryFinderDelegate Get = (provider, config, name) =>
        {
            var client = provider.GetService<IDiscoveryClient>();

            if (config.Type?.ToLower() == "eureka" && client != null)
            {
                return new Eureka(name, client);
            }

            return null;
        };
    }
}
