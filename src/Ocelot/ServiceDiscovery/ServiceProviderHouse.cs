using System.Collections.Generic;
using Ocelot.Responses;

namespace Ocelot.ServiceDiscovery
{
     public class ServiceProviderHouse : IServiceProviderHouse
    {
        private Dictionary<string, Ocelot.ServiceDiscovery.IServiceProvider> _serviceProviders;

        public ServiceProviderHouse()
        {
            _serviceProviders = new Dictionary<string, Ocelot.ServiceDiscovery.IServiceProvider>();
        }

        public Response<IServiceProvider> Get(string key)
        {
            IServiceProvider serviceProvider;
            if(_serviceProviders.TryGetValue(key, out serviceProvider))
            {
                return new OkResponse<Ocelot.ServiceDiscovery.IServiceProvider>(serviceProvider);
            }

            return new ErrorResponse<IServiceProvider>(new List<Ocelot.Errors.Error>()
            {
                new UnableToFindServiceProviderError($"unabe to find service provider for {key}")
            });
        }
        public Response Add(string key, IServiceProvider serviceProvider)
        {
            _serviceProviders[key] = serviceProvider;
            return new OkResponse();
        }
    }
}