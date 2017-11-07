using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.LoadBalancer.LoadBalancers;

namespace Ocelot.LoadBalancer
{
    public class LoadBalancerCreator : ILoadBalancerCreator
    {
        private readonly ILoadBalancerHouse _loadBalancerHouse;
        private readonly ILoadBalancerFactory _loadBalanceFactory;

        public LoadBalancerCreator(ILoadBalancerHouse loadBalancerHouse, ILoadBalancerFactory loadBalancerFactory)
        {
            _loadBalancerHouse = loadBalancerHouse;
            _loadBalanceFactory = loadBalancerFactory;
        }
    
        public async Task SetupLoadBalancer(ReRoute reRoute)
        {
            var loadBalancer = await _loadBalanceFactory.Get(reRoute);
            _loadBalancerHouse.Add(reRoute.ReRouteKey, loadBalancer);
        }
    }
}