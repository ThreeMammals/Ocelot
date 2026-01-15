namespace Ocelot.RateLimiting;

public readonly record struct ClientRequestIdentity(string ClientId, string LoadBalancerKey)
{
    public override string ToString() => $"{ClientId}:{LoadBalancerKey}";
}
