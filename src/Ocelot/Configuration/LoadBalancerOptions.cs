using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;
using Ocelot.LoadBalancer.Balancers;

namespace Ocelot.Configuration;

public class LoadBalancerOptions
{
    public LoadBalancerOptions()
    {
        Type = nameof(NoLoadBalancer);
    }

    public LoadBalancerOptions(FileLoadBalancerOptions options)
        : this(options?.Type, options?.Key, options?.Expiry)
    { }

    public LoadBalancerOptions(string type, string key, int? expiryInMs)
    {
        Type = type.IfEmpty(nameof(NoLoadBalancer));
        Key = nameof(CookieStickySessions).Equals(type, StringComparison.InvariantCultureIgnoreCase)
            ? key.IfEmpty(CookieStickySessions.DefSessionCookieName)
            : key;
        ExpiryInMs = nameof(CookieStickySessions).Equals(type, StringComparison.InvariantCultureIgnoreCase)
            ? expiryInMs ?? CookieStickySessions.DefSessionExpiryMilliseconds
            : expiryInMs ?? 0;
    }

    public string Type { get; init; }
    public string Key { get; init; }
    public int ExpiryInMs { get; init; }
}
