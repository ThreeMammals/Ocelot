namespace Ocelot.Provider.Consul;

public class ConsulClientFactory : IConsulClientFactory
{
    public IConsulClient Get(ConsulRegistryConfiguration config)
        => new ConsulClient(c => OverrideConfig(c, config));

    private static void OverrideConfig(ConsulClientConfiguration to, ConsulRegistryConfiguration from)
    {
        to.Address = new Uri($"{from.Scheme}://{from.Host}:{from.Port}");

        if (!string.IsNullOrEmpty(from?.Token))
        {
            to.Token = from.Token;
        }
    }
}
