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
            foreach (var upstreamHttpMethod in fileRoute.UpstreamHttpMethod)
            {
                Append(upstreamHttpMethod, ',');
            }

            Append(fileRoute.UpstreamPathTemplate);

            // Other properties are optional, use constants for missing values to aid debugging
            Append(CoalesceNullOrWhiteSpace(fileRoute.UpstreamHost, "no-host"));
            Append(CoalesceNullOrWhiteSpace(fileRoute.ServiceNamespace, "no-svc-ns"));
            Append(CoalesceNullOrWhiteSpace(fileRoute.ServiceName, "no-svc-name"));
            Append(CoalesceNullOrWhiteSpace(fileRoute.LoadBalancerOptions.Type, "no-lb-type"));
            Append(CoalesceNullOrWhiteSpace(fileRoute.LoadBalancerOptions.Key, "no-lb-key"));

            return keyBuilder.ToString();

            // Helper function for appending a part to the key, automatically inserts a separator as needed
            void Append(string next, char separator = '|')
            {
                if (keyBuilder.Length > 0)
                {
                    keyBuilder.Append(separator);
                }

                keyBuilder.Append(next);
            }

            string CoalesceNullOrWhiteSpace(string first, string second) => string.IsNullOrWhiteSpace(first) ? second : first;
        }
    }
}
