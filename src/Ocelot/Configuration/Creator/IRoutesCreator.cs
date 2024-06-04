using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IRoutesCreator
{
    List<Route> Create(FileConfiguration fileConfiguration);
    
    /// <summary>
    /// Creates a timeout value for a given file route based on the global configuration.
    /// </summary>
    /// <param name="route">The file route for which to create the timeout.</param>
    /// <param name="global">The global configuration to use for creating the timeout.</param>
    /// <returns>The timeout value in seconds.</returns>
    int CreateTimeout(FileRoute route, FileGlobalConfiguration global);
}
