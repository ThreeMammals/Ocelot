using System.Threading.Tasks;
using Ocelot.Configuration;

namespace Ocelot.LoadBalancer
{
    public interface ILoadBalancerCreator
    {
        Task SetupLoadBalancer(ReRoute reRoute);
    }
}