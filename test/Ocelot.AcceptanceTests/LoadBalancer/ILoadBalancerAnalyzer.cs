using Ocelot.LoadBalancer;
using Ocelot.Values;
using System.Collections.Concurrent;

namespace Ocelot.AcceptanceTests.LoadBalancer;

internal interface ILoadBalancerAnalyzer
{
    ConcurrentBag<LeaseEventArgs> Events { get; }
    object Analyze();
    Dictionary<ServiceHostAndPort, int> GetHostCounters();
    bool HasManyServiceGenerations(int maxGeneration);
    int BottomOfConnections();
    int TopOfConnections();
}
