using System.Collections.Generic;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public interface IServiceDiscoveryProvider
    {
         List<Service> Get();
    }
}