namespace Ocelot.LoadBalancer.LoadBalancers
{
    using System.Collections.Generic;
    using System.Linq;
    using Ocelot.Configuration;
    using Ocelot.Responses;
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

        public Response<ILoadBalancer> Get(DownstreamReRoute reRoute, ServiceProviderConfiguration config)
        {
            var serviceProviderFactoryResponse = _serviceProviderFactory.Get(config, reRoute);

            if (serviceProviderFactoryResponse.IsError)
            {
                return new ErrorResponse<ILoadBalancer>(serviceProviderFactoryResponse.Errors);
            }

            var serviceProvider = serviceProviderFactoryResponse.Data;
            var requestedType = reRoute.LoadBalancerOptions?.Type ?? nameof(NoLoadBalancer);
            var applicableCreator = _loadBalancerCreators.Single(c => c.Type == requestedType);
            var createdLoadBalancer = applicableCreator.Create(reRoute, serviceProvider);
            return new OkResponse<ILoadBalancer>(createdLoadBalancer);
        }
    }
}
