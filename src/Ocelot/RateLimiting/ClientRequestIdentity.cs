namespace Ocelot.RateLimiting;

public readonly record struct ClientRequestIdentity(string ClientId, string LoadBalancerKey)
{
    public override string ToString() => $"{ClientId}:{LoadBalancerKey}";
}
{
    public ClientRequestIdentity(string clientId, string loadBalancerKey)
    {
        ClientId = clientId;
        LoadBalancerKey = loadBalancerKey;
    }

    public string ClientId;
    public string LoadBalancerKey;

    public override readonly string ToString() => $"{ClientId}:{LoadBalancerKey}";
}
