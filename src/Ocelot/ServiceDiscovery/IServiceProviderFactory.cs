using Ocelot.Configuration;

namespace Ocelot.ServiceDiscovery
{
    public interface IServiceProviderFactory
    {
        Ocelot.ServiceDiscovery.IServiceProvider Get(ReRoute reRoute);
    }
}