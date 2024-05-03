using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

/// <summary>
/// This class implements the <see cref="IMetadataCreator"/> interface.
/// </summary>
public class DefaultMetadataCreator : IMetadataCreator
{
    public MetadataOptions Create(IDictionary<string, string> metadata, FileGlobalConfiguration fileGlobalConfiguration)
    {
        // metadata from the route could be null when no metadata is defined
        metadata ??= new Dictionary<string, string>();

        // metadata from the global configuration is never null
        var mergedMetadata = new Dictionary<string, string>(fileGlobalConfiguration.MetadataOptions.Metadata);

        foreach (var (key, value) in metadata)
        {
            mergedMetadata[key] = value;
        }

        return new MetadataOptionsBuilder()
            .WithMetadata(mergedMetadata)
            .WithSeparators(fileGlobalConfiguration.MetadataOptions.Separators)
            .WithTrimChars(fileGlobalConfiguration.MetadataOptions.TrimChars)
            .WithStringSplitOption(fileGlobalConfiguration.MetadataOptions.StringSplitOption)
            .WithNumberStyle(fileGlobalConfiguration.MetadataOptions.NumberStyle)
            .WithCurrentCulture(fileGlobalConfiguration.MetadataOptions.CurrentCulture)
            .Build();
    }
}
