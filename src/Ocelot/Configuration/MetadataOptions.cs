using Ocelot.Configuration.File;
using System.Globalization;

namespace Ocelot.Configuration;

public class MetadataOptions
{
    public MetadataOptions(MetadataOptions from)
    {
        Separators = from.Separators;
        TrimChars = from.TrimChars;
        StringSplitOption = from.StringSplitOption;
        NumberStyle = from.NumberStyle;
        CurrentCulture = from.CurrentCulture;
        Metadata = from.Metadata;
    }

    public MetadataOptions(FileMetadataOptions from)
    {
        StringSplitOption = Enum.Parse<StringSplitOptions>(from.StringSplitOption);
        NumberStyle = Enum.Parse<NumberStyles>(from.NumberStyle);
        CurrentCulture = CultureInfo.GetCultureInfo(from.CurrentCulture);
        Separators = from.Separators;
        TrimChars = from.TrimChars;
        Metadata = from.Metadata;
    }

    public MetadataOptions(string[] separators, char[] trimChars, StringSplitOptions stringSplitOption,
        NumberStyles numberStyle, CultureInfo currentCulture, IDictionary<string, string> metadata)
    {
        Separators = separators;
        TrimChars = trimChars;
        StringSplitOption = stringSplitOption;
        NumberStyle = numberStyle;
        CurrentCulture = currentCulture;
        Metadata = metadata;
    }

    public string[] Separators { get; }
    public char[] TrimChars { get; }
    public StringSplitOptions StringSplitOption { get; }
    public NumberStyles NumberStyle { get; }
    public CultureInfo CurrentCulture { get; }
    public IDictionary<string, string> Metadata { get; set; }
}
