using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;
using System.Linq;

namespace Ocelot.Configuration.Creator
{
    public class ReRouteKeyCreator : IReRouteKeyCreator
    {
        public string Create(FileReRoute fileReRoute)
        {
            if (IsStickySession(fileReRoute))
            {
                return $"{nameof(CookieStickySessions)}:{fileReRoute.LoadBalancerOptions.Key}";
            }

            return $"{fileReRoute.UpstreamPathTemplate}|{string.Join(",", fileReRoute.UpstreamHttpMethod)}|{string.Join(",", fileReRoute.DownstreamHostAndPorts.Select(x => $"{x.Host}:{x.Port}"))}";
        }

        private bool IsStickySession(FileReRoute fileReRoute)
        {
            if (!string.IsNullOrEmpty(fileReRoute.LoadBalancerOptions.Type)
                && !string.IsNullOrEmpty(fileReRoute.LoadBalancerOptions.Key)
                && fileReRoute.LoadBalancerOptions.Type == nameof(CookieStickySessions))
            {
                return true;
            }

            return false;
        }
    }
}
