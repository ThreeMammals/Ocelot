using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class MetadataCreator : IMetadataCreator
{
    public MetadataOptions Create(IDictionary<string, string> metadata, FileGlobalConfiguration fileGlobalConfiguration)
    {
        var mergedMetadata = fileGlobalConfiguration.MetadataOptions.Metadata.Any()
            ? new Dictionary<string, string>(fileGlobalConfiguration.MetadataOptions.Metadata)
            : new Dictionary<string, string>();

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
