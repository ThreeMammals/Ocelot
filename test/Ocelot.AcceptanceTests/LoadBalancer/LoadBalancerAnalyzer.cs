using Microsoft.AspNetCore.Http;
using Ocelot.LoadBalancer;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;
using System.Collections.Concurrent;

namespace Ocelot.AcceptanceTests.LoadBalancer;

internal class LoadBalancerAnalyzer : ILoadBalancerAnalyzer, ILoadBalancer
{
    protected readonly string _serviceName;
    protected LoadBalancerAnalyzer(string serviceName) => _serviceName = serviceName;

    public string ServiceName => _serviceName;
    public virtual string GenerationPrefix => "Gen:";
    public ConcurrentBag<LeaseEventArgs> Events { get; } = new();

    public virtual object Analyze()
    {
        var allGenerations = Events
            .Select(e => e.Service.Tags.FirstOrDefault(t => t.StartsWith(GenerationPrefix)))
            .Where(generation => !string.IsNullOrEmpty(generation))
            .Distinct().ToArray();
        var allIndices = Events.Select(e => e.ServiceIndex)
            .Distinct().OrderBy(index => index).ToArray();

        Dictionary<string, List<LeaseEventArgs>> eventsPerGeneration = new();
        foreach (var generation in allGenerations)
        {
            var l = Events.Where(e => e.Service.Tags.Contains(generation)).ToList();
            eventsPerGeneration.Add(generation, l);
        }

        Dictionary<string, List<int>> generationIndices = new();
        foreach (var generation in allGenerations)
        {
            var l = eventsPerGeneration[generation].Select(e => e.ServiceIndex).Distinct().ToList();
            generationIndices.Add(generation, l);
        }

        Dictionary<string, List<Lease>> generationLeases = new();
        foreach (var generation in allGenerations)
        {
            var l = eventsPerGeneration[generation].Select(e => e.Lease).ToList();
            generationLeases.Add(generation, l);
        }

        Dictionary<string, List<ServiceHostAndPort>> generationHosts = new();
        foreach (var generation in allGenerations)
        {
            var l = eventsPerGeneration[generation].Select(e => e.Lease.HostAndPort).Distinct().ToList();
            generationHosts.Add(generation, l);
        }

        Dictionary<string, List<Lease>> generationLeasesWithMaxConnections = new();
        foreach (var generation in allGenerations)
        {
            List<Lease> leases = new();
            var uniqueHosts = generationHosts[generation];
            foreach (var host in uniqueHosts)
            {
                int max = generationLeases[generation].Where(l => l == host).Max(l => l.Connections);
                Lease wanted = generationLeases[generation].Find(l => l == host && l.Connections == max);
                leases.Add(wanted);
            }

            leases = leases.OrderBy(l => l.HostAndPort.DownstreamPort).ToList();
            generationLeasesWithMaxConnections.Add(generation, leases);
        }

        return generationLeasesWithMaxConnections;
    }

    public virtual bool HasManyServiceGenerations(int maxGeneration)
    {
        int[] generations = new int[maxGeneration + 1];
        string[] tags = new string[maxGeneration + 1];
        for (int i = 0; i < generations.Length; i++)
        {
            generations[i] = i;
            tags[i] = GenerationPrefix + i;
        }

        var all = Events
            .Select(e => e.Service.Tags.FirstOrDefault(t => t.StartsWith(GenerationPrefix)))
            .Distinct().ToArray();
        return all.All(tags.Contains);
    }

    public virtual Dictionary<ServiceHostAndPort, int> GetHostCounters()
    {
        var hosts = Events.Select(e => e.Lease.HostAndPort).Distinct().ToList();
        var grouping = Events
            .GroupBy(e => e.Lease.HostAndPort)
            .OrderBy(g => g.Key.DownstreamPort);
        return ToHostCountersDictionary(grouping);
    }

    public virtual Dictionary<ServiceHostAndPort, int> ToHostCountersDictionary(IEnumerable<IGrouping<ServiceHostAndPort, LeaseEventArgs>> grouping)
        => grouping.ToDictionary(g => g.Key, g => g.Count(e => e.Lease == g.Key));

    public virtual int BottomOfConnections()
    {
        var hostCounters = GetHostCounters();
        return hostCounters.Min(_ => _.Value);
    }

    public virtual int TopOfConnections()
    {
        var hostCounters = GetHostCounters();
        return hostCounters.Max(_ => _.Value);
    }

    public virtual string Type => nameof(LoadBalancerAnalyzer);
    public virtual Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext) => Task.FromResult<Response<ServiceHostAndPort>>(new ErrorResponse<ServiceHostAndPort>(new UnableToFindLoadBalancerError(GetType().Name)));
    public virtual void Release(ServiceHostAndPort hostAndPort) { }
}
