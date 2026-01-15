using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

/// <summary>
/// This interface describes the creation of metadata options.
/// </summary>
public interface IMetadataCreator
{
    MetadataOptions Create(IDictionary<string, string> metadata, FileGlobalConfiguration globalConfiguration);
}
