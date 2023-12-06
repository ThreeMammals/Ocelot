using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class MetadataCreator : IMetadataCreator
{
    public Dictionary<string, string> Create(Dictionary<string, string> routeMetadata, FileGlobalConfiguration fileGlobalConfiguration)
    {
        var metadata = fileGlobalConfiguration?.Metadata != null
            ? new Dictionary<string, string>(fileGlobalConfiguration.Metadata)
            : new();

        if (routeMetadata != null)
        {
            foreach (var (key, value) in routeMetadata)
            {
                if (metadata.ContainsKey(key))
                {
                    // Replace the global value by the one in file route
                    metadata[key] = value;
                }
                else
                {
                    metadata.Add(key, value);
                }
            }
        }

        return metadata;
    }
}
