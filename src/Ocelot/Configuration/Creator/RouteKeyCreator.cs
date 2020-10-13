using System.Linq;

using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;

namespace Ocelot.Configuration.Creator
{
    public class RouteKeyCreator : IRouteKeyCreator
    {
        public string Create(FileRoute fileRoute) => IsStickySession(fileRoute)
            ? $"{nameof(CookieStickySessions)}:{fileRoute.LoadBalancerOptions.Key}"
            : $"{fileRoute.UpstreamPathTemplate}|{ToUpstreamHttpMethodPart(fileRoute)}|{ToDownstreamHostPart(fileRoute)}";

        private static bool IsStickySession(FileRoute fileRoute) =>
            !string.IsNullOrEmpty(fileRoute.LoadBalancerOptions.Type)
            && !string.IsNullOrEmpty(fileRoute.LoadBalancerOptions.Key)
            && fileRoute.LoadBalancerOptions.Type == nameof(CookieStickySessions);

        private static string ToDownstreamHostPart(FileRoute fileRoute)
            => string.Join(",", fileRoute.DownstreamHostAndPorts
                .Select(x => string.IsNullOrWhiteSpace(x.GlobalHostKey) ? $"{x.Host}:{x.Port}": x.GlobalHostKey));

        private static string ToUpstreamHttpMethodPart(FileRoute fileRoute)
            => string.Join(",", fileRoute.UpstreamHttpMethod);
    }
}
