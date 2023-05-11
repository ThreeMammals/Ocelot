using System.Linq;

using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;

namespace Ocelot.Configuration.Creator
{
    public class RouteKeyCreator : IRouteKeyCreator
    {
        public string Create(FileRoute fileRoute) => IsStickySession(fileRoute)
            ? $"{nameof(CookieStickySessions)}:{fileRoute.LoadBalancerOptions.Key}"
            : $"{fileRoute.UpstreamPathTemplate}|{string.Join(',', fileRoute.UpstreamHttpMethod)}|{string.Join(',', fileRoute.DownstreamHostAndPorts.Select(x => $"{x.Host}:{x.Port}"))}";

        private static bool IsStickySession(FileRoute fileRoute) =>
            !string.IsNullOrEmpty(fileRoute.LoadBalancerOptions.Type)
            && !string.IsNullOrEmpty(fileRoute.LoadBalancerOptions.Key)
            && fileRoute.LoadBalancerOptions.Type == nameof(CookieStickySessions);
    }
}
