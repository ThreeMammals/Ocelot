using Ocelot.Configuration.File;
using System.Globalization;

namespace Ocelot.Configuration;

public class MetadataOptions
{
    public MetadataOptions()
    {
        CurrentCulture = CultureInfo.CurrentCulture;
        NumberStyle = NumberStyles.Any;
        Separators = new[] { "," };
        StringSplitOption = StringSplitOptions.None;
        TrimChars = new[] { ' ' };
        Metadata = new Dictionary<string, string>();
    }

    public MetadataOptions(MetadataOptions from)
    {
        CurrentCulture = from.CurrentCulture;
        NumberStyle = from.NumberStyle;
        Separators = from.Separators;
        StringSplitOption = from.StringSplitOption;
        TrimChars = from.TrimChars;
        Metadata = from.Metadata;
    }

    public MetadataOptions(FileMetadataOptions from)
    {
        CurrentCulture = CultureInfo.GetCultureInfo(from.CurrentCulture);
        NumberStyle = Enum.Parse<NumberStyles>(from.NumberStyle);
        Separators = from.Separators;
        StringSplitOption = Enum.Parse<StringSplitOptions>(from.StringSplitOption);
        TrimChars = from.TrimChars;
        Metadata = new Dictionary<string, string>();
    }

    public MetadataOptions(string[] separators, char[] trimChars, StringSplitOptions stringSplitOption,
        NumberStyles numberStyle, CultureInfo currentCulture, IDictionary<string, string> metadata)
    {
        CurrentCulture = currentCulture;
        NumberStyle = numberStyle;
        Separators = separators;
        StringSplitOption = stringSplitOption;
        TrimChars = trimChars;
        Metadata = metadata;
    }

    public CultureInfo CurrentCulture { get; }
    public NumberStyles NumberStyle { get; }
    public string[] Separators { get; }
    public StringSplitOptions StringSplitOption { get; }
    public char[] TrimChars { get; }
    public IDictionary<string, string> Metadata { get; set; }
}
