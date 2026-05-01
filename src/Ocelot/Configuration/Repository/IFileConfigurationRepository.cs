using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Repository;

public interface IFileConfigurationRepository
{
    FileConfiguration Get();
    Task<FileConfiguration> GetAsync(CancellationToken cancellationToken = default);

    void Set(FileConfiguration configuration);
    Task SetAsync(FileConfiguration configuration, CancellationToken cancellationToken = default);
}
