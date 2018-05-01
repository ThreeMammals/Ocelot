using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Middleware;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class NoLoadBalancer : ILoadBalancer
    {
        private readonly List<Service> _services;

        public NoLoadBalancer(List<Service> services)
        {
            _services = services;
        }

        public async Task<Response<ServiceHostAndPort>> Lease(DownstreamContext downstreamContext)
        {
            //todo no point spinning a task up here, also first or default could be null..
            if (_services == null || _services.Count == 0)
            {
                return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError("There were no services in NoLoadBalancer"));
            }

            var service = await Task.FromResult(_services.FirstOrDefault());
            return new OkResponse<ServiceHostAndPort>(service.HostAndPort);
        }

        public void Release(ServiceHostAndPort hostAndPort)
        {
        }
    }
}
