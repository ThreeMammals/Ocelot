using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class MetadataCreator : IMetadataCreator
{
    public IDictionary<string, string> Create(IDictionary<string, string> routeMetadata, FileGlobalConfiguration fileGlobalConfiguration)
    {
        var metadata = fileGlobalConfiguration?.Metadata != null
            ? new Dictionary<string, string>(fileGlobalConfiguration.Metadata)
            : new Dictionary<string, string>();

        if (routeMetadata == null)
        {
            return metadata;
        }

        foreach (var (key, value) in routeMetadata)
        {
            metadata[key] = value;
        }

        return metadata;
    }
}
