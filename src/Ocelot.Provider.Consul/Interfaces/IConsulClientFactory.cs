namespace Ocelot.Provider.Consul.Interfaces;

public interface IConsulClientFactory
{
    IConsulClient Get(ConsulRegistryConfiguration config);
}
