using Ocelot.Configuration;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public interface ILoadBalancerFactory
    {
        ILoadBalancer Get(ReRoute reRoute);
    }
}