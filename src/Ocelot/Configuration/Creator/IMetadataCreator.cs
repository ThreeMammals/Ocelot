using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IMetadataCreator
{
    IDictionary<string, string> Create(IDictionary<string, string> metadata, FileGlobalConfiguration fileGlobalConfiguration);
}
