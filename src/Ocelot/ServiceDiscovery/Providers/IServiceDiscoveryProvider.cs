using Ocelot.Values;

namespace Ocelot.ServiceDiscovery.Providers
{
    public interface IServiceDiscoveryProvider
    {
        Task<List<Service>> GetAsync();
    }
}
