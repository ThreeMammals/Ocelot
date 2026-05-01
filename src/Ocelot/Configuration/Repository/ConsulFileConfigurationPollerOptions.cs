using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Repository;

public class ConsulFileConfigurationPollerOptions : IFileConfigurationPollerOptions
{
    private readonly IInternalConfigurationRepository _internalRepository;
    private readonly IFileConfigurationRepository _fileRepository;

    public ConsulFileConfigurationPollerOptions(IInternalConfigurationRepository internalRepo, IFileConfigurationRepository fileRepo)
    {
        _internalRepository = internalRepo;
        _fileRepository = fileRepo;
    }

    public const int DefaultDelay = 1000;

    public int Delay()
    {
        var configuration = _fileRepository.Get();
        return GetDelay(configuration);
    }

    public async Task<int> DelayAsync(CancellationToken cancellationToken = default)
    {
        var configuration = await _fileRepository.GetAsync(cancellationToken);
        return GetDelay(configuration);
    }

    private int GetDelay(FileConfiguration configuration)
    {
        var discoveryOpts = configuration?.GlobalConfiguration?.ServiceDiscoveryProvider;
        if (discoveryOpts != null && discoveryOpts.PollingInterval > 0)
            return discoveryOpts.PollingInterval;

        var internalConfig = _internalRepository.Get();
        var discoveryConfig = internalConfig?.ServiceProviderConfiguration;
        return (discoveryConfig != null && discoveryConfig.PollingInterval > 0)
            ? discoveryConfig.PollingInterval
            : DefaultDelay;
    }
}
