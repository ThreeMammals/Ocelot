using Ocelot.Cache;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure;
using Ocelot.Values;

namespace Ocelot.Configuration.Creator;

public class UpstreamTemplatePatternCreator : IUpstreamTemplatePatternCreator
{
    public const string RegExMatchZeroOrMoreOfEverything = ".*";
    private const string RegExMatchOneOrMoreOfEverythingUntilNextForwardSlash = "[^/]+";
    private const string RegExMatchEndString = "$";
    private const string RegExIgnoreCase = "(?i)";
    private const string RegExForwardSlashOnly = "^/$";
    private const string RegExForwardSlashAndOnePlaceHolder = "^/.*";
    private readonly IOcelotCache<Regex> _cache;

    public UpstreamTemplatePatternCreator(IOcelotCache<Regex> cache)
    {
        _cache = cache;
    }

    public UpstreamPathTemplate Create(IRoute route)
    {
        var upstreamTemplate = route.UpstreamPathTemplate;
        var placeholders = new List<string>();

        for (var i = 0; i < upstreamTemplate.Length; i++)
        {
            if (IsPlaceHolder(upstreamTemplate, i))
            {
                var postitionOfPlaceHolderClosingBracket = upstreamTemplate.IndexOf('}', i);
                var difference = postitionOfPlaceHolderClosingBracket - i + 1;
                var placeHolderName = upstreamTemplate.Substring(i, difference);
                placeholders.Add(placeHolderName);

                // Hack to handle /{url} case
                if (ForwardSlashAndOnePlaceHolder(upstreamTemplate, placeholders, postitionOfPlaceHolderClosingBracket))
                {
                    return CreateTemplate(RegExForwardSlashAndOnePlaceHolder, 0, false, route.UpstreamPathTemplate);
                }
            }
        }

        var containsQueryString = false;

        if (upstreamTemplate.Contains('?'))
        {
            containsQueryString = true;
            upstreamTemplate = upstreamTemplate.Replace(
                upstreamTemplate.Contains("/?") ? "/?" : "?",
                @"(/$|/\?|\?|$)");
        }

        for (var i = 0; i < placeholders.Count; i++)
        {
            var indexOfPlaceholder = upstreamTemplate.IndexOf(placeholders[i], StringComparison.Ordinal);
            var indexOfNextForwardSlash = upstreamTemplate.IndexOf("/", indexOfPlaceholder, StringComparison.Ordinal);
            if (indexOfNextForwardSlash < indexOfPlaceholder || (containsQueryString && upstreamTemplate.IndexOf('?', StringComparison.Ordinal) < upstreamTemplate.IndexOf(placeholders[i], StringComparison.Ordinal)))
            {
                upstreamTemplate = upstreamTemplate.Replace(placeholders[i], RegExMatchZeroOrMoreOfEverything);
            }
            else
            {
                upstreamTemplate = upstreamTemplate.Replace(placeholders[i], RegExMatchOneOrMoreOfEverythingUntilNextForwardSlash);
            }
        }

        if (upstreamTemplate == "/")
        {
            return CreateTemplate(RegExForwardSlashOnly, route.Priority, containsQueryString, route.UpstreamPathTemplate);
        }

        var index = upstreamTemplate.LastIndexOf('/'); // index of last forward slash
        if (index < (upstreamTemplate.Length - 1) && upstreamTemplate[index + 1] == '.')
        {
            upstreamTemplate = upstreamTemplate[..index] + "(?:|/" + upstreamTemplate[++index..] + ")";
        }

        if (upstreamTemplate.EndsWith("/"))
        {
            upstreamTemplate = upstreamTemplate.Remove(upstreamTemplate.Length - 1, 1) + "(/|)";
        }

        var template = route.RouteIsCaseSensitive
            ? $"^{upstreamTemplate}{RegExMatchEndString}"
            : $"^{RegExIgnoreCase}{upstreamTemplate}{RegExMatchEndString}";

        return CreateTemplate(template, route.Priority, containsQueryString, route.UpstreamPathTemplate);
    }

    /// <summary>Time-to-live for caching <see cref="Regex"/> to initialize the <see cref="UpstreamPathTemplate.Pattern"/> property.</summary>
    /// <value>A constant <see cref="TimeSpan"/> structure, default absolute value is 1 minute.</value>
    public static TimeSpan RegexCachingTTL { get; set; } = TimeSpan.FromMinutes(1.0D);

    protected Regex GetRegex(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        if (!_cache.TryGetValue(key, nameof(UpstreamPathTemplate), out var rgx))
        {
            rgx = RegexGlobal.New(key, RegexOptions.Singleline);
            _cache.Add(key, rgx, RegexCachingTTL, nameof(UpstreamPathTemplate));
        }

        return rgx;
    }

    protected UpstreamPathTemplate CreateTemplate(string template, int priority, bool containsQueryString, string originalValue)
        => new(template, priority, containsQueryString, originalValue)
        {
            Pattern = GetRegex(template),
        };

    private static bool ForwardSlashAndOnePlaceHolder(string upstreamTemplate, List<string> placeholders, int postitionOfPlaceHolderClosingBracket)
        => upstreamTemplate.Substring(0, 2) == "/{" &&
            placeholders.Count == 1 &&
            upstreamTemplate.Length == postitionOfPlaceHolderClosingBracket + 1;

    private static bool IsPlaceHolder(string upstreamTemplate, int i)
        => upstreamTemplate[i] == '{';
}
