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
        metadata ??= new Dictionary<string, string>();
        globalConfiguration.Metadata ??= new Dictionary<string, string>();
        var merged = new Dictionary<string, string>(globalConfiguration.Metadata);
        foreach (var (key, value) in metadata)
        {
            merged[key] = value;
        }

        var options = globalConfiguration.MetadataOptions;
        return new MetadataOptionsBuilder()
            .WithMetadata(merged)
            .WithSeparators(options.Separators)
            .WithTrimChars(options.TrimChars)
            .WithStringSplitOption(options.StringSplitOption)
            .WithNumberStyle(options.NumberStyle)
            .WithCurrentCulture(options.CurrentCulture)
            .Build();
    }
}
