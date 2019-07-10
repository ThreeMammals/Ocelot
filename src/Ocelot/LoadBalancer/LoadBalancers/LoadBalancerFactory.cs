namespace Ocelot.LoadBalancer.LoadBalancers
{
    using System.Collections.Generic;
    using System.Linq;
    using Ocelot.Configuration;
    using Ocelot.Responses;
    using System.Threading.Tasks;
    using Ocelot.ServiceDiscovery;

    public class LoadBalancerFactory : ILoadBalancerFactory
    {
        private readonly IServiceDiscoveryProviderFactory _serviceProviderFactory;
        private readonly IEnumerable<ILoadBalancerCreator> _loadBalancerCreators;

        public LoadBalancerFactory(IServiceDiscoveryProviderFactory serviceProviderFactory, IEnumerable<ILoadBalancerCreator> loadBalancerCreators)
        {
            _serviceProviderFactory = serviceProviderFactory;
            _loadBalancerCreators = loadBalancerCreators;
        }

        public Task<Response<ILoadBalancer>> Get(DownstreamReRoute reRoute, ServiceProviderConfiguration config)
        {
            var serviceProviderFactoryResponse = _serviceProviderFactory.Get(config, reRoute);

            Response<ILoadBalancer> response;
            if (serviceProviderFactoryResponse.IsError)
            {
                response = new ErrorResponse<ILoadBalancer>(serviceProviderFactoryResponse.Errors);
            }
            else
            {
                var serviceProvider = serviceProviderFactoryResponse.Data;
                var requestedType = reRoute.LoadBalancerOptions?.Type ?? nameof(NoLoadBalancer);
                var applicableCreator = _loadBalancerCreators.Single(c => c.Type == requestedType);
                var createdLoadBalancer = applicableCreator.Create(reRoute, serviceProvider);
                response = new OkResponse<ILoadBalancer>(createdLoadBalancer);
            }

            return Task.FromResult(response);
        }
    }
}
