using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.Configuration.Repository;

/// <summary>
/// Feature: <see cref="Features.AddOcelotConfigurationRepository(IServiceCollection)"/>.
/// </summary>
/// <remarks>
/// Feature Commit: <see href="https://github.com/ThreeMammals/Ocelot/commit/aa0d8fe59a6e41226f6ed4d9b827fa5f9b3dc6fe">aa0d8fe</see>.<br/>
/// Feature PR: <see href="https://github.com/ThreeMammals/Ocelot/pull/157/">157</see>.
/// </remarks>
public interface IFileConfigurationSetter
{
    Task SetAsync(FileConfiguration configuration, CancellationToken cancellationToken = default);
}
