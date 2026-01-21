using Ocelot.Infrastructure;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher;

public class PlaceholderNameAndValue
{
    private const char OpeningBrace = Placeholders.OpeningBrace;
    private const char ClosingBrace = Placeholders.ClosingBrace;

    public PlaceholderNameAndValue(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }
    public string Value { get; }

    public string Key { get => Name.Trim(OpeningBrace, ClosingBrace); }
    public override string ToString() => $"[{Name}={Value}]";
}
