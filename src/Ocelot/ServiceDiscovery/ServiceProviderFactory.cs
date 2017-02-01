using System;
using Ocelot.Configuration;

namespace Ocelot.ServiceDiscovery
{
    public class ServiceProviderFactory : IServiceProviderFactory
    {
        public  Ocelot.ServiceDiscovery.IServiceProvider Get(ReRoute reRoute)
        {
            throw new NotImplementedException();
        }
    }
}