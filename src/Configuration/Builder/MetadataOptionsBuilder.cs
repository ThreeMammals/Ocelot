using System.Globalization;

namespace Ocelot.Configuration.Builder;

public class MetadataOptionsBuilder
{
    private string[] _separators;
    private char[] _trimChars;
    private StringSplitOptions _stringSplitOption;
    private NumberStyles _numberStyle;
    private CultureInfo _currentCulture;
    private IDictionary<string, string> _metadata;

    public MetadataOptionsBuilder WithSeparators(string[] separators)
    {
        _separators = separators;
        return this;
    }

    public MetadataOptionsBuilder WithTrimChars(char[] trimChars)
    {
        _trimChars = trimChars;
        return this;
    }

    public MetadataOptionsBuilder WithStringSplitOption(string stringSplitOption)
    {
        _stringSplitOption = Enum.Parse<StringSplitOptions>(stringSplitOption);
        return this;
    }

    public MetadataOptionsBuilder WithNumberStyle(string numberStyle)
    {
        _numberStyle = Enum.Parse<NumberStyles>(numberStyle);
        return this;
    }

    public MetadataOptionsBuilder WithCurrentCulture(string currentCulture)
    {
        _currentCulture = CultureInfo.GetCultureInfo(currentCulture);
        return this;
    }

    public MetadataOptionsBuilder WithMetadata(IDictionary<string, string> metadata)
    {
        _metadata = metadata;
        return this;
    }

    public MetadataOptions Build()
    {
        return new MetadataOptions(_separators, _trimChars, _stringSplitOption, _numberStyle, _currentCulture, _metadata);
    }
}
