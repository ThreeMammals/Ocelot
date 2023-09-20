﻿using global::Consul;

namespace Ocelot.Provider.Consul
{
    public interface IConsulClientFactory
    {
        IConsulClient Get(ConsulRegistryConfiguration config);
    }
}
