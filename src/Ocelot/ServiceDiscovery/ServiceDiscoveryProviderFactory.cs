using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Configuration;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class ServiceDiscoveryProviderFactory : IServiceDiscoveryProviderFactory
    {
        private readonly IServiceProvider _provider;
        private readonly ServiceDiscoveryFinderDelegate _delegates;
        private readonly IOcelotLogger _logger;

        public ServiceDiscoveryProviderFactory(IOcelotLoggerFactory factory, IServiceProvider provider)
        {
            _provider = provider;
            _delegates = provider.GetService<ServiceDiscoveryFinderDelegate>();
            _logger = factory.CreateLogger<ServiceDiscoveryProviderFactory>();
        }

        public Response<IServiceDiscoveryProvider> Get(ServiceProviderConfiguration serviceConfig, DownstreamRoute route)
        {
            if (route.UseServiceDiscovery)
            {
                var routeName = route.UpstreamPathTemplate?.Template ?? route.ServiceName ?? string.Empty;
                _logger.LogInformation(() => $"The {nameof(DownstreamRoute.UseServiceDiscovery)} mode of the route '{routeName}' is enabled.");
                return GetServiceDiscoveryProvider(serviceConfig, route);
            }

            var services = route.DownstreamAddresses
                .Select(address => new Service(
                    route.ServiceName,
                    new ServiceHostAndPort(address.Host, address.Port, route.DownstreamScheme),
                    string.Empty,
                    string.Empty,
                    Enumerable.Empty<string>()))
                .ToList();

            return new OkResponse<IServiceDiscoveryProvider>(new ConfigurationServiceProvider(services));
        }

        private Response<IServiceDiscoveryProvider> GetServiceDiscoveryProvider(ServiceProviderConfiguration config, DownstreamRoute route)
        {
            _logger.LogInformation(() => $"Getting service discovery provider of {nameof(config.Type)} '{config.Type}'...");

            if (config.Type?.ToLower() == "servicefabric")
            {
                var sfConfig = new ServiceFabricConfiguration(config.Host, config.Port, route.ServiceName);
                return new OkResponse<IServiceDiscoveryProvider>(new ServiceFabricServiceDiscoveryProvider(sfConfig));
            }

            if (_delegates != null)
            {
                var provider = _delegates?.Invoke(_provider, config, route);

                if (provider.GetType().Name.ToLower() == config.Type.ToLower())
                {
                    return new OkResponse<IServiceDiscoveryProvider>(provider);
                }
            }

            var message = $"Unable to find service discovery provider for {nameof(config.Type)}: '{config.Type}'!";
            _logger.LogWarning(() => $"Unable to find service discovery provider for {nameof(config.Type)}: '{config.Type}'!");

            return new ErrorResponse<IServiceDiscoveryProvider>(new UnableToFindServiceDiscoveryProviderError(message));
        }
    }
}
