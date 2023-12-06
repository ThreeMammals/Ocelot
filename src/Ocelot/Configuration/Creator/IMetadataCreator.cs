using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IMetadataCreator
{
    Dictionary<string, string> Create(Dictionary<string, string> metadata, FileGlobalConfiguration fileGlobalConfiguration);
}
