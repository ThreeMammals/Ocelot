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
        bool isStickySessions = nameof(CookieStickySessions).Equals(type, StringComparison.OrdinalIgnoreCase);
        Key = isStickySessions
            ? key.IfEmpty(CookieStickySessions.DefSessionCookieName)
            : key;
        ExpiryInMs = isStickySessions
            ? expiryInMs ?? CookieStickySessions.DefSessionExpiryMilliseconds
            : expiryInMs ?? 0;
    }

    public string Type { get; init; }
    public string Key { get; init; }
    public int ExpiryInMs { get; init; }
}
