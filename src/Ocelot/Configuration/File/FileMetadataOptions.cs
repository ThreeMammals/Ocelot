using System.Globalization;

namespace Ocelot.Configuration.File;

public class FileMetadataOptions
{
    public FileMetadataOptions()
    {
        Separators = new[] { "," };
        TrimChars = new[] { ' ' };
        StringSplitOption = Enum.GetName(typeof(StringSplitOptions), StringSplitOptions.None);
        NumberStyle = Enum.GetName(typeof(NumberStyles), NumberStyles.Any);
        CurrentCulture = CultureInfo.CurrentCulture.Name;
        Metadata = new Dictionary<string, string>();
    }

    public FileMetadataOptions(FileMetadataOptions from)
    {
        Separators = from.Separators;
        TrimChars = from.TrimChars;
        StringSplitOption = from.StringSplitOption;
        NumberStyle = from.NumberStyle;
        CurrentCulture = from.CurrentCulture;
        Metadata = from.Metadata;
    }

    public IDictionary<string, string> Metadata { get; set; }
    public string[] Separators { get; set; }
    public char[] TrimChars { get; set; }
    public string StringSplitOption { get; set; }
    public string NumberStyle { get; set; }
    public string CurrentCulture { get; set; }
}
