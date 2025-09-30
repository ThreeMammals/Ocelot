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

    public LoadBalancerOptions(FileLoadBalancerOptions fromOptions)
    {
        ArgumentNullException.ThrowIfNull(fromOptions);
        Type = fromOptions.Type.IfEmpty(nameof(NoLoadBalancer));
        Key = fromOptions.Key.IfEmpty(CookieStickySessions.DefSessionCookieName);
        ExpiryInMs = fromOptions.Expiry ?? CookieStickySessions.DefSessionExpiryMilliseconds;
    }

    public LoadBalancerOptions(string type, string key, int? expiryInMs)
    {
        Type = type.IfEmpty(nameof(NoLoadBalancer));
        Key = key.IfEmpty(CookieStickySessions.DefSessionCookieName);
        ExpiryInMs = expiryInMs ?? CookieStickySessions.DefSessionExpiryMilliseconds;
    }

    public string Type { get; init; }
    public string Key { get; init; }
    public int ExpiryInMs { get; init; }
}
