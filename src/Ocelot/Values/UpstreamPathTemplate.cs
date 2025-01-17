using Ocelot.Infrastructure;

namespace Ocelot.Values;

/// <summary>The model to keep data of upstream path.</summary>
public partial class UpstreamPathTemplate
{
#if NET7_0_OR_GREATER
    [GeneratedRegex("$^", RegexOptions.Singleline, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex RegexNoTemplate();
#else
    private static readonly Regex _regexNoTemplate = RegexGlobal.New("$^", RegexOptions.Singleline);
    private static Regex RegexNoTemplate() => _regexNoTemplate;
#endif

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
