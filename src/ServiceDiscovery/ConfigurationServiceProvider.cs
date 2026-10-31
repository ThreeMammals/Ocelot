using Ocelot.Values;

namespace Ocelot.ServiceDiscovery;

public class ConfigurationServiceProvider : IServiceDiscoveryProvider
{
    private readonly List<Service> _services;

    public ConfigurationServiceProvider(List<Service> services) => _services = services;

    public string Name => nameof(ConfigurationServiceProvider);

    public Task<List<Service>> GetAsync() => ValueTask.FromResult(_services).AsTask();
}
