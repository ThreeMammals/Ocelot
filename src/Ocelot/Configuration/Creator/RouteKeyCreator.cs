using Ocelot.Configuration.File;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Infrastructure.Extensions;
using Ocelot.LoadBalancer.Balancers;

namespace Ocelot.Configuration.Creator;

public class RouteKeyCreator : IRouteKeyCreator
{
    public const char Separator = '|';
    public const char Dot = DiscoveryDownstreamRouteFinder.Dot;

    /// <summary>
    /// Creates the unique <see langword="string"/> key based on the route properties for load balancing etc.
    /// </summary>
    /// <remarks>
    /// Key template: <c>UpstreamHttpMethod|UpstreamPathTemplate|UpstreamHost|DownstreamHostAndPorts|ServiceNamespace|ServiceName|LoadBalancerType|LoadBalancerKey</c>.
    /// </remarks>
    /// <param name="route">The route object.</param>
    /// <param name="loadBalancing">Final options for load balancing.</param>
    /// <returns>A <see langword="string"/> object containing the key.</returns>
    public string Create(FileRoute route, LoadBalancerOptions loadBalancing)
    {
        if (TryStickySession(loadBalancing, out var stickySessionKey))
        {
            return stickySessionKey;
        }

        var keyBuilder = new StringBuilder()
            .AppendNext(route.UpstreamHttpMethod.Csv()) // required
            .AppendNext(route.UpstreamPathTemplate) // required
            .AppendNext(route.UpstreamHost.IfEmpty("no-host")) // optional...
            .AppendNext(route.DownstreamHostAndPorts.Select(AsString).Csv().IfEmpty("no-host-and-port"))
            .AppendNext(route.ServiceNamespace.IfEmpty("no-svc-ns"))
            .AppendNext(route.ServiceName.IfEmpty("no-svc-name"))
            .AppendNext(loadBalancing.Type.IfEmpty("no-lb-type"))
            .AppendNext(loadBalancing.Key.IfEmpty("no-lb-key"));
        return keyBuilder.ToString();
    }

    public string Create(FileDynamicRoute route, LoadBalancerOptions loadBalancing)
    {
        if (TryStickySession(loadBalancing, out var stickySessionKey))
        {
            return stickySessionKey;
        }

        // it should be constructed in upper contexts
        return !loadBalancing.Key.IsEmpty() ? loadBalancing.Key
            : Create(route.ServiceNamespace, route.ServiceName, loadBalancing);
    }

    public string Create(string serviceNamespace, string serviceName, LoadBalancerOptions loadBalancing)
    {
        if (TryStickySession(loadBalancing, out var stickySessionKey))
        {
            return stickySessionKey;
        }

        return !loadBalancing.Key.IsEmpty() ? loadBalancing.Key
            : string.Join(Dot, serviceNamespace, serviceName); // upstreamHttpMethod ?
    }

    protected virtual bool TryStickySession(LoadBalancerOptions loadBalancing, out string stickySessionKey)
    {
        bool isStickySession = nameof(CookieStickySessions).Equals(loadBalancing.Type, StringComparison.OrdinalIgnoreCase)
            && loadBalancing.Key.Length > 0;
        stickySessionKey = isStickySession
            ? $"{nameof(CookieStickySessions)}:{loadBalancing.Key}"
            : string.Empty;
        return isStickySession;
    }

    public static string AsString(FileHostAndPort host) => host?.ToString();
}

internal static class RouteKeyCreatorHelpers
{
    /// <summary>Helper function to append a string to the key builder, separated by a pipe.</summary>
    /// <param name="builder">The builder of the key.</param>
    /// <param name="next">The next word to add.</param>
    /// <returns>The reference to the builder.</returns>
    public static StringBuilder AppendNext(this StringBuilder builder, string next)
    {
        if (builder.Length > 0)
        {
            builder.Append(RouteKeyCreator.Separator);
        }

        return builder.Append(next);
    }
}
