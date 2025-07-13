using Ocelot.Provider.Consul.Interfaces;

namespace Ocelot.Provider.Consul;

public class ConsulClientFactory : IConsulClientFactory
{
    // TODO We need this overloaded method -> 
    //public IConsulClient Get(ServiceProviderConfiguration config)
    public IConsulClient Get(ConsulRegistryConfiguration config)
        => new ConsulClient(c => OverrideConfig(c, config));

    // TODO ->
    //private static void OverrideConfig(ConsulClientConfiguration to, ServiceProviderConfiguration from)
    // Factory which consumes concrete types is a bad factory! A more abstract types are required
    private static void OverrideConfig(ConsulClientConfiguration to, ConsulRegistryConfiguration from) // TODO Why ConsulRegistryConfiguration? We use ServiceProviderConfiguration props only! :)
    {
        to.Address = new Uri($"{from.Scheme}://{from.Host}:{from.Port}");

        if (!string.IsNullOrEmpty(from?.Token))
        {
            to.Token = from.Token;
        }
    }
}
