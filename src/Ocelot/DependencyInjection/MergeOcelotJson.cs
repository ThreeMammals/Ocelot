namespace Ocelot.DependencyInjection;

public enum MergeOcelotJson
{
    /// <summary>
    /// The option to merge all configuration files to one primary config file aka ocelot.json.
    /// </summary>
    ToFile = 0,

    /// <summary>
    /// The option to merge all configuration files to memory and reuse the config by in-memory configuration provider.
    /// </summary>
    ToMemory = 1,
}
