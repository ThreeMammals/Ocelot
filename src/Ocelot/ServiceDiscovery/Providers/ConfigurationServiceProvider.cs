using Ocelot.Values;

namespace Ocelot.ServiceDiscovery.Providers;

public class ConfigurationServiceProvider : IServiceDiscoveryProvider
{
    private readonly List<Service> _services;

    public ConfigurationServiceProvider(List<Service> services) => _services = services;

    public Task<List<Service>> GetAsync() => ValueTask.FromResult(_services).AsTask();
}
