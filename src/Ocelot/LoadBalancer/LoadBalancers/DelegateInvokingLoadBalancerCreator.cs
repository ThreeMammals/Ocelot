namespace Ocelot.LoadBalancer.LoadBalancers
{
    using System;
    using Ocelot.Configuration;
    using Ocelot.ServiceDiscovery.Providers;
    using Ocelot.Responses;

    public class DelegateInvokingLoadBalancerCreator<T> : ILoadBalancerCreator
        where T : ILoadBalancer
    {
        private readonly Func<DownstreamReRoute, IServiceDiscoveryProvider, ILoadBalancer> _creatorFunc;

        public DelegateInvokingLoadBalancerCreator(
            Func<DownstreamReRoute, IServiceDiscoveryProvider, ILoadBalancer> creatorFunc)
        {
            _creatorFunc = creatorFunc;
        }

        public Response<ILoadBalancer> Create(DownstreamReRoute reRoute, IServiceDiscoveryProvider serviceProvider)
        {
            try
            {
                return new OkResponse<ILoadBalancer>(_creatorFunc(reRoute, serviceProvider));

            }
            catch (Exception e)
            {
                return new ErrorResponse<ILoadBalancer>(new ErrorInvokingLoadBalancerCreator(e));
            }
        }

        public string Type => typeof(T).Name;
    }
}
