using System;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class StickySession
    {
        public StickySession(ServiceHostAndPort hostAndPort, DateTime expiry)
        {
            HostAndPort = hostAndPort;
            Expiry = expiry;
        }

        public ServiceHostAndPort HostAndPort { get; }
        
        public DateTime Expiry { get; }
    }
}
