using System.Globalization;

namespace Ocelot.Configuration.File;

public class FileMetadataOptions
{
    public FileMetadataOptions()
    {
        CurrentCulture = CultureInfo.CurrentCulture.Name;
        NumberStyle = Enum.GetName(NumberStyles.Any);
        Separators = new[] { "," };
        StringSplitOption = Enum.GetName(StringSplitOptions.None);
        TrimChars = new[] { ' ' };
    }

    public FileMetadataOptions(FileMetadataOptions from)
    {
        CurrentCulture = from.CurrentCulture;
        NumberStyle = from.NumberStyle;
        Separators = from.Separators;
        StringSplitOption = from.StringSplitOption;
        TrimChars = from.TrimChars;
    }

    public string CurrentCulture { get; set; }
    public string NumberStyle { get; set; }
    public string[] Separators { get; set; }
    public string StringSplitOption { get; set; }
    public char[] TrimChars { get; set; }
}
