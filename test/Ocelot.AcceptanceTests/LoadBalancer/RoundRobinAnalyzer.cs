using KubeClient.Models;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Values;
using System.Collections.Concurrent;

namespace Ocelot.AcceptanceTests.LoadBalancer;

internal class RoundRobinAnalyzer : RoundRobin, ILoadBalancer
{
    public readonly ConcurrentBag<LeaseEventArgs> Events = new();

    public RoundRobinAnalyzer(Func<Task<List<Service>>> services, string serviceName)
        : base(services, serviceName)
    {
        this.Leased += Me_Leased;
    }

    private void Me_Leased(object sender, LeaseEventArgs e) => Events.Add(e);

    public const string GenerationPrefix = nameof(EndpointsV1.Metadata.Generation) + ":";

    public object Analyze()
    {
        var allGenerations = Events
            .Select(e => e.Service.Tags.FirstOrDefault(t => t.StartsWith(GenerationPrefix)))
            .Distinct().ToArray();
        var allIndices = Events.Select(e => e.ServiceIndex)
            .Distinct().ToArray();

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

    public bool HasManyServiceGenerations(int maxGeneration)
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

    public Dictionary<ServiceHostAndPort, int> GetHostCounters()
    {
        var hosts = Events.Select(e => e.Lease.HostAndPort).Distinct().ToList();
        return Events
            .GroupBy(e => e.Lease.HostAndPort)
            .ToDictionary(g => g.Key, g => g.Max(e => e.Lease.Connections));
    }

    public int BottomOfConnections()
    {
        var hostCounters = GetHostCounters();
        return hostCounters.Min(_ => _.Value);
    }

    public int TopOfConnections()
    {
        var hostCounters = GetHostCounters();
        return hostCounters.Max(_ => _.Value);
    }
}
