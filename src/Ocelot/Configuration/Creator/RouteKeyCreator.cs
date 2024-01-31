using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;

namespace Ocelot.Configuration.Creator
{
    public class RouteKeyCreator : IRouteKeyCreator
    {
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

            // Build the key from the route's properties using the format:
            // UpstreamHttpMethod|UpstreamPathTemplate|UpstreamHost|ServiceNamespace|ServiceName|LoadBalancerType|LoadBalancerKey
            var keyBuilder = new StringBuilder();

            // UpstreamHttpMethod and UpstreamPathTemplate are required
            var upstreamHttpMethods = Csv(fileRoute.UpstreamHttpMethod);
            Append(keyBuilder, upstreamHttpMethods);
            Append(keyBuilder, fileRoute.UpstreamPathTemplate);

            // Other properties are optional, replace undefined values with defaults to aid debugging
            Append(keyBuilder, Coalesce(fileRoute.UpstreamHost, "no-host"));

            var downstreamHostAndPorts = Csv(fileRoute.DownstreamHostAndPorts.Select(downstream => $"{downstream.Host}:{downstream.Port}"));
            Append(keyBuilder, Coalesce(downstreamHostAndPorts, "no-host-and-port"));
            Append(keyBuilder, Coalesce(fileRoute.ServiceNamespace, "no-svc-ns"));
            Append(keyBuilder, Coalesce(fileRoute.ServiceName, "no-svc-name"));
            Append(keyBuilder, Coalesce(fileRoute.LoadBalancerOptions.Type, "no-lb-type"));
            Append(keyBuilder, Coalesce(fileRoute.LoadBalancerOptions.Key, "no-lb-key"));

            return keyBuilder.ToString();

            // Helper function to append a string to the keyBuilder, separated by a pipe
            static void Append(StringBuilder keyBuilder, string next)
            {
                if (keyBuilder.Length > 0)
                {
                    keyBuilder.Append('|');
                }

                keyBuilder.Append(next);
            }

            // Helper function to convert multiple strings into a comma-separated string
            static string Csv(IEnumerable<string> values) => string.Join(',', values);

            // Helper function to return the first non-null-or-whitespace string
            static string Coalesce(string first, string second) => string.IsNullOrWhiteSpace(first) ? second : first;
        }
    }
}
