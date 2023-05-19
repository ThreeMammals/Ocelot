﻿namespace Ocelot.LoadBalancer.LoadBalancers
{
    using System;

    using Configuration;

    using Responses;

    using ServiceDiscovery.Providers;

    public class DelegateInvokingLoadBalancerCreator<T> : ILoadBalancerCreator
        where T : ILoadBalancer
    {
        private readonly Func<DownstreamRoute, IServiceDiscoveryProvider, ILoadBalancer> _creatorFunc;

        public DelegateInvokingLoadBalancerCreator(
            Func<DownstreamRoute, IServiceDiscoveryProvider, ILoadBalancer> creatorFunc)
        {
            _creatorFunc = creatorFunc;
        }

        public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
        {
            try
            {
                return new OkResponse<ILoadBalancer>(_creatorFunc(route, serviceProvider));

            }
            catch (Exception e)
            {
                return new ErrorResponse<ILoadBalancer>(new ErrorInvokingLoadBalancerCreator(e));
            }
        }

        public string Type => typeof(T).Name;
    }
}
