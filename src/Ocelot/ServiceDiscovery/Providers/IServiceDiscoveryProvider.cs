using System.Collections.Generic;
using System.Threading.Tasks;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery.Providers
{
    public interface IServiceDiscoveryProvider
    {
         Task<List<Service>> Get();
    }
}
