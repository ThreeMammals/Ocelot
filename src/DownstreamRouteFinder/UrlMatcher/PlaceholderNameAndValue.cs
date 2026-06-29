using Ocelot.Infrastructure;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher;

public class PlaceholderNameAndValue(string name, string value, bool isKey = false)
{
    private const char OpeningBrace = Placeholders.OpeningBrace;
    private const char ClosingBrace = Placeholders.ClosingBrace;

    public string Name { get; } = isKey ? string.Concat(OpeningBrace, name, ClosingBrace) : name;
    public string Value { get; } = value;

    public string Key { get => Name.Trim(OpeningBrace, ClosingBrace); }
    public override string ToString() => $"[{Name}={Value}]";
}
