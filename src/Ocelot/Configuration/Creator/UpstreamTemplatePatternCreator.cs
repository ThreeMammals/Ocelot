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
    private const string QuerySeparatorPattern = @"(/$|/\?|\?|$)";
    private readonly IOcelotCache<Regex> _cache;

    public UpstreamTemplatePatternCreator(IOcelotCache<Regex> cache)
    {
        _cache = cache;
    }

    public UpstreamPathTemplate Create(IRouteUpstream route)
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
        string querySeparator = null;

        if (upstreamTemplate.Contains('?'))
        {
            containsQueryString = true;
            querySeparator = upstreamTemplate.Contains("/?") ? "/?" : "?";
        }

        upstreamTemplate = EscapeLiteralSegments(upstreamTemplate, placeholders, querySeparator);

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
        if (index < (upstreamTemplate.Length - 1) && StartsWithEscapedDot(upstreamTemplate, index))
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
            _cache.Add(key, rgx, nameof(UpstreamPathTemplate), RegexCachingTTL);
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

    private static bool StartsWithEscapedDot(string upstreamTemplate, int slashIndex)
        => upstreamTemplate[slashIndex + 1] == '.'
           || (slashIndex < upstreamTemplate.Length - 2
               && upstreamTemplate[slashIndex + 1] == '\\'
               && upstreamTemplate[slashIndex + 2] == '.');

    private static string EscapeLiteralSegments(string upstreamTemplate, List<string> placeholders, string querySeparator)
    {
        if (placeholders.Count == 0 && string.IsNullOrEmpty(querySeparator))
        {
            return Regex.Escape(upstreamTemplate);
        }

        var builder = new StringBuilder();
        var index = 0;

        while (index < upstreamTemplate.Length)
        {
            var (segmentIndex, segmentLength, segmentValue) = FindNextPreservedSegment(upstreamTemplate, placeholders, querySeparator, index);
            if (segmentIndex < 0)
            {
                builder.Append(Regex.Escape(upstreamTemplate[index..]));
                break;
            }

            if (segmentIndex > index)
            {
                builder.Append(Regex.Escape(upstreamTemplate[index..segmentIndex]));
            }

            builder.Append(segmentValue);
            index = segmentIndex + segmentLength;
        }

        return builder.ToString();
    }

    private static (int Index, int Length, string Value) FindNextPreservedSegment(
        string upstreamTemplate,
        List<string> placeholders,
        string querySeparator,
        int startIndex)
    {
        var segmentIndex = -1;
        var segmentLength = 0;
        var segmentValue = string.Empty;

        foreach (var candidate in placeholders)
        {
            var index = upstreamTemplate.IndexOf(candidate, startIndex, StringComparison.Ordinal);
            if (index >= 0 && (segmentIndex < 0 || index < segmentIndex))
            {
                segmentIndex = index;
                segmentLength = candidate.Length;
                segmentValue = candidate;
            }
        }

        if (!string.IsNullOrEmpty(querySeparator))
        {
            var index = upstreamTemplate.IndexOf(querySeparator, startIndex, StringComparison.Ordinal);
            if (index >= 0 && (segmentIndex < 0 || index < segmentIndex))
            {
                segmentIndex = index;
                segmentLength = querySeparator.Length;
                segmentValue = QuerySeparatorPattern;
            }
        }

        return (segmentIndex, segmentLength, segmentValue);
    }
}
