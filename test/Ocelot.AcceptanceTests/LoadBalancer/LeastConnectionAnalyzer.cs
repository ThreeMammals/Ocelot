﻿using Microsoft.AspNetCore.Http;
using Ocelot.LoadBalancer;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.AcceptanceTests.LoadBalancer;

internal sealed class LeastConnectionAnalyzer : LoadBalancerAnalyzer, ILoadBalancer
{
    private readonly LeastConnection loadBalancer;

    public LeastConnectionAnalyzer(Func<Task<List<Service>>> services, string serviceName)
    {
        loadBalancer = new(services, serviceName);
        loadBalancer.Leased += Me_Leased;
    }

    private void Me_Leased(object sender, LeaseEventArgs args) => Events.Add(args);

    public Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext) => loadBalancer.Lease(httpContext);
    public void Release(ServiceHostAndPort hostAndPort) => loadBalancer.Release(hostAndPort);

    public override Dictionary<ServiceHostAndPort, int> ToHostCountersDictionary(IEnumerable<IGrouping<ServiceHostAndPort, LeaseEventArgs>> grouping)
        => grouping.ToDictionary(g => g.Key, g => g.Count(e => e.Lease == g.Key));
}
