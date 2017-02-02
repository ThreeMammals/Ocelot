using System;
using System.Collections.Generic;
using Ocelot.Responses;

namespace Ocelot.ServiceDiscovery
{
    public interface IServiceProviderHouse
    {
        Response<IServiceProvider> Get(string key);
        Response Add(string key, IServiceProvider serviceProvider);
    }
}