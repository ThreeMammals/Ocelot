using Ocelot.Infrastructure;

namespace Ocelot.Values;

/// <summary>The model to keep data of upstream path.</summary>
public partial class UpstreamPathTemplate
{
    [GeneratedRegex("$^", RegexOptions.Singleline, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex RegexNoTemplate();
    private static readonly ConcurrentDictionary<string, Regex> _regex = new();

    public UpstreamPathTemplate(string template, int priority, bool containsQueryString, string originalValue)
    {
        Template = template;
        Priority = priority;
        ContainsQueryString = containsQueryString;
        OriginalValue = originalValue;
        Pattern = template == null ? RegexNoTemplate() :
            _regex.AddOrUpdate(template,
                RegexGlobal.New(template, RegexOptions.Singleline),
                (key, oldValue) => oldValue);
    }

    public string Template { get; }

    public int Priority { get; }

    public bool ContainsQueryString { get; }

    public string OriginalValue { get; }

    public Regex Pattern { get; }
}
