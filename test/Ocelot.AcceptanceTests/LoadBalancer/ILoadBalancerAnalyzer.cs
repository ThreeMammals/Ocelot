using Ocelot.LoadBalancer;
using Ocelot.Values;
using System.Collections.Concurrent;

namespace Ocelot.AcceptanceTests.LoadBalancer;

public interface ILoadBalancerAnalyzer
{
    string ServiceName { get; }
    string GenerationPrefix { get; }
    ConcurrentBag<LeaseEventArgs> Events { get; }
    object Analyze();
    Dictionary<ServiceHostAndPort, int> GetHostCounters();
    Dictionary<ServiceHostAndPort, int> ToHostCountersDictionary(IEnumerable<IGrouping<ServiceHostAndPort, LeaseEventArgs>> grouping);
    bool HasManyServiceGenerations(int maxGeneration);
    int BottomOfConnections();
    int TopOfConnections();
}
