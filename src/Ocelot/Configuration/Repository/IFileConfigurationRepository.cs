using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Repository;

public interface IFileConfigurationRepository
{
    /// <summary>
    /// Gets file configuration, aka ocelot.json content model.
    /// </summary>
    /// <returns>A <see cref="FileConfiguration"/> model.</returns>
    Task<FileConfiguration> GetAsync();

    /// <summary>
    /// Sets file configuration rewriting ocelot.json content.
    /// </summary>
    /// <param name="fileConfiguration">Current model.</param>
    /// <returns>A <see cref="Task"/> object.</returns>
    Task SetAsync(FileConfiguration fileConfiguration);
}
