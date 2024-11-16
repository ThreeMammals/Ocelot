using Ocelot.Configuration.File;
using Ocelot.Infrastructure;
using Ocelot.Values;

namespace Ocelot.Configuration.Creator;

/// <summary>
/// Default creator of upstream templates based on route headers.
/// </summary>
/// <remarks>Ocelot feature: Routing based on request header.</remarks>
public partial class UpstreamHeaderTemplatePatternCreator : IUpstreamHeaderTemplatePatternCreator
{
    private const string PlaceHolderPattern = @"(\{header:.*?\})";
#if NET7_0_OR_GREATER
    [GeneratedRegex(PlaceHolderPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline, RegexGlobal.DefaultMatchTimeoutMilliseconds, "en-US")]
    private static partial Regex RegexPlaceholders();
#else
    private static readonly Regex _regexPlaceholders = RegexGlobal.New(PlaceHolderPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static Regex RegexPlaceholders() => _regexPlaceholders;
#endif

    public IDictionary<string, UpstreamHeaderTemplate> Create(IRoute route)
    {
        var result = new Dictionary<string, UpstreamHeaderTemplate>();

        foreach (var headerTemplate in route.UpstreamHeaderTemplates)
        {
            var headerTemplateValue = headerTemplate.Value;
            var matches = RegexPlaceholders().Matches(headerTemplateValue);

            if (matches.Count > 0)
            {
                var placeholders = matches.Select(m => m.Groups[1].Value).ToArray();
                for (int i = 0; i < placeholders.Length; i++)
                {
                    var indexOfPlaceholder = headerTemplateValue.IndexOf(placeholders[i]);
                    var placeholderName = placeholders[i][8..^1]; // remove "{header:" and "}"
                    headerTemplateValue = headerTemplateValue.Replace(placeholders[i], $"(?<{placeholderName}>.+)");
                }
            }

            var template = route.RouteIsCaseSensitive
                ? $"^{headerTemplateValue}$"
                : $"^(?i){headerTemplateValue}$"; // ignore case

            result.Add(headerTemplate.Key, new(template, headerTemplate.Value));
        }

        return result;
    }
}
