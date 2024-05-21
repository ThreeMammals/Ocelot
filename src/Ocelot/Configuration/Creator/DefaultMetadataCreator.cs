using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

/// <summary>
/// This class implements the <see cref="IMetadataCreator"/> interface.
/// </summary>
public class DefaultMetadataCreator : IMetadataCreator
{
    public MetadataOptions Create(IDictionary<string, string> metadata, FileGlobalConfiguration globalConfiguration)
    {
        // metadata from the route could be null when no metadata is defined
        metadata ??= new Dictionary<string, string>();

        // metadata from the global configuration is never null
        var options = globalConfiguration.MetadataOptions;
        var mergedMetadata = new Dictionary<string, string>(options.Metadata);

        foreach (var (key, value) in metadata)
        {
            mergedMetadata[key] = value;
        }

        return new MetadataOptionsBuilder()
            .WithMetadata(mergedMetadata)
            .WithSeparators(options.Separators)
            .WithTrimChars(options.TrimChars)
            .WithStringSplitOption(options.StringSplitOption)
            .WithNumberStyle(options.NumberStyle)
            .WithCurrentCulture(options.CurrentCulture)
            .Build();
    }
}
