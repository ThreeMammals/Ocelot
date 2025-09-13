namespace Ocelot.RateLimiting;

public struct ClientRequestIdentity
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
