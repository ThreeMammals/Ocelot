using Ocelot.Responses;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public interface ILoadBalancerHouse
    {
        Response<ILoadBalancer> Get(string key);
        Response Add(string key, ILoadBalancer loadBalancer);
    }
}