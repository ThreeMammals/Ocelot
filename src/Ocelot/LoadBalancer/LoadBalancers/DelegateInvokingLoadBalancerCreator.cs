namespace Ocelot.LoadBalancer.LoadBalancers
{
    using System;
    using Ocelot.Configuration;
    using Ocelot.ServiceDiscovery.Providers;

    public class DelegateInvokingLoadBalancerCreator<T> : ILoadBalancerCreator
        where T : ILoadBalancer
    {
        private readonly Func<DownstreamReRoute, IServiceDiscoveryProvider, ILoadBalancer> _creatorFunc;

        public DelegateInvokingLoadBalancerCreator(
            Func<DownstreamReRoute, IServiceDiscoveryProvider, ILoadBalancer> creatorFunc)
        {
            _creatorFunc = creatorFunc;
        }

        public ILoadBalancer Create(DownstreamReRoute reRoute, IServiceDiscoveryProvider serviceProvider)
        {
            return _creatorFunc(reRoute, serviceProvider);
        }

        public string Type => typeof(T).Name;
    }
}
