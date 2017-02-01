using System.Collections.Generic;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public interface IServiceProvider
    {
         List<Service> Get();
    }
}