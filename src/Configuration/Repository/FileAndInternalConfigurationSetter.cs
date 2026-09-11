using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.Configuration.Repository;

/// <summary>
/// Features: <see cref="Features.AddOcelotConfigurationRepository(IServiceCollection)"/> and <see cref="IOcelotBuilder.AddConfigurationPoller()"/>.
/// </summary>
/// <remarks>
/// Feature Commit: <see href="https://github.com/ThreeMammals/Ocelot/commit/aa0d8fe59a6e41226f6ed4d9b827fa5f9b3dc6fe">aa0d8fe</see>.<br/>
/// Feature PR: <see href="https://github.com/ThreeMammals/Ocelot/pull/157/">157</see>.
/// </remarks>
public class FileAndInternalConfigurationSetter : IFileConfigurationSetter
{
    private readonly IInternalConfigurationRepository _internalConfigRepo;
    private readonly IInternalConfigurationCreator _configCreator;
    private readonly IFileConfigurationRepository _repo;

    public FileAndInternalConfigurationSetter(
        IInternalConfigurationRepository configRepo,
        IInternalConfigurationCreator configCreator,
        IFileConfigurationRepository repo)
    {
        _internalConfigRepo = configRepo;
        _configCreator = configCreator;
        _repo = repo;
    }

    public async Task SetAsync(FileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await _repo.SetAsync(configuration, cancellationToken);

        var config = await _configCreator.Create(configuration);
        if (config.IsError)
            throw new ConfigurationRepositoryException(config.Errors);

        _internalConfigRepo.AddOrReplace(config.Data);
    }
}
