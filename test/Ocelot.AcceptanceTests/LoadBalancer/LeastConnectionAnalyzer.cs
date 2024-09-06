using Ocelot.LoadBalancer;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Values;
using System.Collections.Concurrent;

namespace Ocelot.AcceptanceTests.LoadBalancer;

internal sealed class LeastConnectionAnalyzer : LeastConnection, ILoadBalancerAnalyzer
{
    private readonly LoadBalancerAnalyzer _analyzer;

    public LeastConnectionAnalyzer(Func<Task<List<Service>>> services, string serviceName)
        : base(services, serviceName)
    {
        _analyzer = new();
        this.Leased += Me_Leased;
    }

    private void Me_Leased(object sender, LeaseEventArgs e) => Events.Add(e);

    public ConcurrentBag<LeaseEventArgs> Events => _analyzer.Events;
    public object Analyze() => _analyzer.Analyze();
    public bool HasManyServiceGenerations(int maxGeneration) => _analyzer.HasManyServiceGenerations(maxGeneration);
    public Dictionary<ServiceHostAndPort, int> GetHostCounters() => _analyzer.GetHostCounters();
    public int BottomOfConnections() => _analyzer.BottomOfConnections();
    public int TopOfConnections() => _analyzer.TopOfConnections();
}
