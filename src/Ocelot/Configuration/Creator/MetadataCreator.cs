using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class MetadataCreator : IMetadataCreator
{
    public MetadataOptions Create(IDictionary<string, string> metadata, FileGlobalConfiguration fileGlobalConfiguration)
    {
        return Create(new FileMetadataOptions { Metadata = metadata ?? new Dictionary<string, string>() }, fileGlobalConfiguration);
    }

    public MetadataOptions Create(FileMetadataOptions routeMetadataOptions, FileGlobalConfiguration fileGlobalConfiguration)
    {
        var metadata = fileGlobalConfiguration.MetadataOptions.Metadata.Any()
            ? new Dictionary<string, string>(fileGlobalConfiguration.MetadataOptions.Metadata)
            : new Dictionary<string, string>();

        foreach (var (key, value) in routeMetadataOptions.Metadata)
        {
            metadata[key] = value;
        }

        return new MetadataOptionsBuilder()
            .WithMetadata(metadata)
            .WithSeparators(fileGlobalConfiguration.MetadataOptions.Separators)
            .WithTrimChars(fileGlobalConfiguration.MetadataOptions.TrimChars)
            .WithStringSplitOption(fileGlobalConfiguration.MetadataOptions.StringSplitOption)
            .WithNumberStyle(fileGlobalConfiguration.MetadataOptions.NumberStyle)
            .WithCurrentCulture(fileGlobalConfiguration.MetadataOptions.CurrentCulture)
            .Build();
    }
}
