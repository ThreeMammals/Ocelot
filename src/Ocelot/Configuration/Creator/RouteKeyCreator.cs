using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;
using Ocelot.LoadBalancer.Balancers;

namespace Ocelot.Configuration.Creator;

public class RouteKeyCreator : IRouteKeyCreator
{
    /// <summary>
    /// Creates the unique <see langword="string"/> key based on the route properties for load balancing etc.
    /// </summary>
    /// <remarks>
    /// Key template:
    /// <list type="bullet">
    /// <item>UpstreamHttpMethod|UpstreamPathTemplate|UpstreamHost|DownstreamHostAndPorts|ServiceNamespace|ServiceName|LoadBalancerType|LoadBalancerKey</item>
    /// </list>
    /// </remarks>
    /// <param name="route">The route object.</param>
    /// <returns>A <see langword="string"/> object containing the key.</returns>
    public string Create(FileRoute route)
    {
        var isStickySession = route.LoadBalancerOptions is
        {
            Type: nameof(CookieStickySessions),
            Key.Length: > 0
        };

        if (isStickySession)
        {
            return $"{nameof(CookieStickySessions)}:{route.LoadBalancerOptions.Key}";
        }

        var keyBuilder = new StringBuilder()
            .AppendNext(route.UpstreamHttpMethod.Csv()) // required
            .AppendNext(route.UpstreamPathTemplate) // required
            .AppendNext(route.UpstreamHost.IfEmpty("no-host")) // optional...
            .AppendNext(route.DownstreamHostAndPorts.Select(AsString).Csv().IfEmpty("no-host-and-port"))
            .AppendNext(route.ServiceNamespace.IfEmpty("no-svc-ns"))
            .AppendNext(route.ServiceName.IfEmpty("no-svc-name"))
            .AppendNext((route.LoadBalancerOptions?.Type).IfEmpty("no-lb-type"))
            .AppendNext((route.LoadBalancerOptions?.Key).IfEmpty("no-lb-key"));
        return keyBuilder.ToString();
    }

    private static string AsString(FileHostAndPort host) => host?.ToString();
}

internal static class RouteKeyCreatorHelpers
{
    /// <summary>
    /// Helper function to append a string to the key builder, separated by a pipe.
    /// </summary>
    /// <param name="builder">The builder of the key.</param>
    /// <param name="next">The next word to add.</param>
    /// <returns>The reference to the builder.</returns>
    public static StringBuilder AppendNext(this StringBuilder builder, string next)
    {
        if (builder.Length > 0)
        {
            builder.Append('|');
        }

        return builder.Append(next);
    }

    /// <summary>
    /// Helper function to convert multiple strings into a comma-separated string.
    /// </summary>
    /// <param name="values">The collection of strings to join by comma separator.</param>
    /// <returns>A <see langword="string"/> in the comma-separated format.</returns>
    public static string Csv(this IEnumerable<string> values) => string.Join(',', values);
}
