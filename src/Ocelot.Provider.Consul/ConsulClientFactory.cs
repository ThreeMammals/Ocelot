using global::Consul;
using System;

namespace Ocelot.Provider.Consul
{
    public class ConsulClientFactory : IConsulClientFactory
    {
        public IConsulClient Get(ConsulRegistryConfiguration config)
        {
            return new ConsulClient(c =>
            {
                c.Address = new Uri($"{config.Scheme}://{config.Host}:{config.Port}");

                if (!string.IsNullOrEmpty(config?.Token))
                {
                    c.Token = config.Token;
                }
            });
        }
    }
}
