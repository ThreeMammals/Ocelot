using Ocelot.Configuration.File;
using System.Globalization;

namespace Ocelot.Configuration;

public class MetadataOptions
{
    public MetadataOptions(MetadataOptions from)
    {
        CurrentCulture = from.CurrentCulture;
        Metadata = from.Metadata;
        NumberStyle = from.NumberStyle;
        Separators = from.Separators;
        StringSplitOption = from.StringSplitOption;
        TrimChars = from.TrimChars;
    }

    public MetadataOptions(FileMetadataOptions from)
    {
        CurrentCulture = CultureInfo.GetCultureInfo(from.CurrentCulture);
        Metadata = from.Metadata;
        NumberStyle = Enum.Parse<NumberStyles>(from.NumberStyle);
        Separators = from.Separators;
        StringSplitOption = Enum.Parse<StringSplitOptions>(from.StringSplitOption);
        TrimChars = from.TrimChars;
    }

    public MetadataOptions(string[] separators, char[] trimChars, StringSplitOptions stringSplitOption,
        NumberStyles numberStyle, CultureInfo currentCulture, IDictionary<string, string> metadata)
    {
        CurrentCulture = currentCulture;
        Metadata = metadata;
        NumberStyle = numberStyle;
        Separators = separators;
        StringSplitOption = stringSplitOption;
        TrimChars = trimChars;
    }

    public CultureInfo CurrentCulture { get; }
    public NumberStyles NumberStyle { get; }
    public string[] Separators { get; }
    public StringSplitOptions StringSplitOption { get; }
    public char[] TrimChars { get; }
    public IDictionary<string, string> Metadata { get; set; }
}
