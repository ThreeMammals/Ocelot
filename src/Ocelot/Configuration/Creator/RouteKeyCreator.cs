using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;

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
    /// <param name="fileRoute">The route object.</param>
    /// <returns>A <see langword="string"/> object containing the key.</returns>
    public string Create(FileRoute fileRoute)
    {
        var isStickySession = fileRoute.LoadBalancerOptions is
        {
            Type: nameof(CookieStickySessions),
            Key.Length: > 0
        };

        if (isStickySession)
        {
            return $"{nameof(CookieStickySessions)}:{fileRoute.LoadBalancerOptions.Key}";
        }

        var upstreamHttpMethods = Csv(fileRoute.UpstreamHttpMethod);
        var downstreamHostAndPorts = Csv(fileRoute.DownstreamHostAndPorts.Select(downstream => $"{downstream.Host}:{downstream.Port}"));

        var keyBuilder = new StringBuilder()

            // UpstreamHttpMethod and UpstreamPathTemplate are required
            .AppendNext(upstreamHttpMethods)
            .AppendNext(fileRoute.UpstreamPathTemplate)

            // Other properties are optional, replace undefined values with defaults to aid debugging
            .AppendNext(Coalesce(fileRoute.UpstreamHost, "no-host"))

            .AppendNext(Coalesce(downstreamHostAndPorts, "no-host-and-port"))
            .AppendNext(Coalesce(fileRoute.ServiceNamespace, "no-svc-ns"))
            .AppendNext(Coalesce(fileRoute.ServiceName, "no-svc-name"))
            .AppendNext(Coalesce(fileRoute.LoadBalancerOptions.Type, "no-lb-type"))
            .AppendNext(Coalesce(fileRoute.LoadBalancerOptions.Key, "no-lb-key"));

        return keyBuilder.ToString();
    }

    /// <summary>
    /// Helper function to convert multiple strings into a comma-separated string.
    /// </summary>
    /// <param name="values">The collection of strings to join by comma separator.</param>
    /// <returns>A <see langword="string"/> in the comma-separated format.</returns>
    private static string Csv(IEnumerable<string> values) => string.Join(',', values);

    /// <summary>
    /// Helper function to return the first non-null-or-whitespace string.
    /// </summary>
    /// <param name="first">The 1st string to check.</param>
    /// <param name="second">The 2nd string to check.</param>
    /// <returns>A <see langword="string"/> which is not empty.</returns>
    private static string Coalesce(string first, string second) => string.IsNullOrWhiteSpace(first) ? second : first;
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
}
