using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration.Repository;

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
            throw new ConfigurationRepositoryException(config.Errors.ToErrorString());

        _internalConfigRepo.AddOrReplace(config.Data);
    }
}
