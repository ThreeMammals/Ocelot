using Ocelot.Infrastructure;

namespace Ocelot.Values;

/// <summary>The model to keep data of upstream path.</summary>
public partial class UpstreamPathTemplate
{
    [GeneratedRegex("$^", RegexOptions.Singleline, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex RegexNoTemplate();

    public UpstreamPathTemplate(string template, int priority, bool containsQueryString, string originalValue)
    {
        Template = template;
        Priority = priority;
        ContainsQueryString = containsQueryString;
        OriginalValue = originalValue;
    }

    public string Template { get; }
    public int Priority { get; }
    public bool ContainsQueryString { get; }
    public string OriginalValue { get; }

    private Regex _pattern;
    public Regex Pattern
    {
        get => _pattern;
        set => _pattern = Template == null || value == null ? RegexNoTemplate() : value;
    }
}
