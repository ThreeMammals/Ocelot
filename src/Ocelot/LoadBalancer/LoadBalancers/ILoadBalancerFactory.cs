namespace Ocelot.LoadBalancer.LoadBalancers
{
    public interface ILoadBalancerFactory
    {
        ILoadBalancer Get(string serviceName, string loadBalancer);
    }
}