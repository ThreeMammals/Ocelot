﻿namespace Ocelot.LoadBalancer.LoadBalancers
{
    using Configuration;

    using Infrastructure;

    using Responses;

    using ServiceDiscovery.Providers;

    public class CookieStickySessionsCreator : ILoadBalancerCreator
    {
        public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
        {
            var loadBalancer = new RoundRobin(async () => await serviceProvider.Get());
            var bus = new InMemoryBus<StickySession>();
            return new OkResponse<ILoadBalancer>(new CookieStickySessions(loadBalancer, route.LoadBalancerOptions.Key,
                route.LoadBalancerOptions.ExpiryInMs, bus));
        }

        public string Type => nameof(CookieStickySessions);
    }
}
