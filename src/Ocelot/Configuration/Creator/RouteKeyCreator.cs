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

            var keyBuilder = new StringBuilder();
            foreach (var upstreamHttpMethod in fileRoute.UpstreamHttpMethod)
            {
                Append(upstreamHttpMethod, ',');
            }

            Append(fileRoute.UpstreamHost);
            Append(fileRoute.UpstreamPathTemplate);

            if (!string.IsNullOrEmpty(fileRoute.ServiceName))
            {
                Append(fileRoute.ServiceNamespace);
                Append(fileRoute.ServiceName);
            }
            else
            {
                foreach (var downstreamHostAndPorts in fileRoute.DownstreamHostAndPorts)
                {
                    Append(downstreamHostAndPorts.Host);
                    Append(downstreamHostAndPorts.Port.ToString(), ':');
                }
            }

            return keyBuilder.ToString();

            // Helper function for appending a part to the key, automatically inserts a separator as needed
            void Append(string next, char separator = '|')
            {
                if (string.IsNullOrEmpty(next))
                {
                    return;
                }

                if (keyBuilder.Length > 0)
                {
                    keyBuilder.Append(separator);
                }

                keyBuilder.Append(next);
            }
        }
    }
}
