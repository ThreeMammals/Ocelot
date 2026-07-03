using Microsoft.AspNetCore.Http;
using Ocelot.LoadBalancer;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.Testing.LoadBalancer;

public sealed class RoundRobinAnalyzer : LoadBalancerAnalyzer, ILoadBalancer
{
    private readonly RoundRobin loadBalancer;

    public RoundRobinAnalyzer(Func<Task<List<Service>>> services, string serviceName)
        : base(serviceName)
    {
        loadBalancer = new(services, serviceName);
        loadBalancer.Leased += Me_Leased;
    }

    private void Me_Leased(object? sender, LeaseEventArgs args) => Events.Add(args);

    public override string Type => nameof(RoundRobinAnalyzer);
    public override Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext) => loadBalancer.LeaseAsync(httpContext);
    public override void Release(ServiceHostAndPort hostAndPort) => loadBalancer.Release(hostAndPort);

    public override string GenerationPrefix => "Generation:";

    public override Dictionary<ServiceHostAndPort, int> ToHostCountersDictionary(IEnumerable<IGrouping<ServiceHostAndPort, LeaseEventArgs>> grouping)
        => grouping.ToDictionary(g => g.Key, g => g.Max(e => e.Lease.Connections));
}
