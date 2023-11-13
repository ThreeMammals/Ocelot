using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Repository;

public interface IFileConfigurationRepository
{
    /// <summary>
    /// Gets file configuration, aka ocelot.json content model.
    /// </summary>
    /// <returns>A <see cref="FileConfiguration"/> model.</returns>
    Task<FileConfiguration> GetAsync();

    Task<Response> Set(FileConfiguration fileConfiguration);
}
