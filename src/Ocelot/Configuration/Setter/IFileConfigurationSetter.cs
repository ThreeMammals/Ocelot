using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Setter;

public interface IFileConfigurationSetter
{
    Task SetAsync(FileConfiguration configuration, CancellationToken cancellationToken = default);
}
