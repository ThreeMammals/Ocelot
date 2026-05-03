using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Repository;

public interface IFileConfigurationSetter
{
    Task SetAsync(FileConfiguration configuration, CancellationToken cancellationToken = default);
}
