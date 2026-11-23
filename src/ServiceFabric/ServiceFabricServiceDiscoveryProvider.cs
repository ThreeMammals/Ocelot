using System.Collections.Generic;
using System.Threading.Tasks;
using Ocelot.ServiceDiscovery;
using Ocelot.Values;

namespace Ocelot.ServiceFabric;

public class ServiceFabricServiceDiscoveryProvider : IServiceDiscoveryProvider
{
    public const string Type = "ServiceFabric";

    public string Name => Type;

    private readonly ServiceFabricConfiguration _configuration;

    public ServiceFabricServiceDiscoveryProvider(ServiceFabricConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<List<Service>> GetAsync()
    {
        return Task.FromResult(new List<Service>
        {
            new(_configuration.ServiceName,
                new ServiceHostAndPort(_configuration.HostName, _configuration.Port),
                "doesnt matter with service fabric",
                "doesnt matter with service fabric",
                new List<string>()),
        });
    }
}
