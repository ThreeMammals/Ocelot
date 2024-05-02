using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IMetadataCreator
{
    MetadataOptions Create(IDictionary<string, string> metadata, FileGlobalConfiguration fileGlobalConfiguration);
}
