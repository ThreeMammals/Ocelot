using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class Lease
    {
        public Lease(ServiceHostAndPort hostAndPort, int connections)
        {
            HostAndPort = hostAndPort;
            Connections = connections;
        }

        public ServiceHostAndPort HostAndPort { get; }
        public int Connections { get; }
    }
}
