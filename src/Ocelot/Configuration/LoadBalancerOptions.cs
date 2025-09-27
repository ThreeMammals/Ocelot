using Ocelot.Infrastructure.Extensions;
using Ocelot.LoadBalancer.Balancers;

namespace Ocelot.Configuration;

public class LoadBalancerOptions
{
    public LoadBalancerOptions()
    {
        Type = nameof(NoLoadBalancer);
    }

    public LoadBalancerOptions(string type, string key, int expiryInMs)
    {
        Type = type.IfEmpty(nameof(NoLoadBalancer));
        Key = key;
        ExpiryInMs = expiryInMs;
    }

    public string Type { get; init; }

    public string Key { get; init; }

    public int ExpiryInMs { get; init; }
}
