using System.Threading.Tasks;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public interface ILoadBalancer
    {
        Task<Response<ServiceHostAndPort>> Lease();
        void Release(ServiceHostAndPort hostAndPort);
    }
}
