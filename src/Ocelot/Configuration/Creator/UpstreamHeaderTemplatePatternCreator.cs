using Ocelot.Configuration.File;
using Ocelot.Values;

namespace Ocelot.Configuration.Creator;

/// <summary>
/// Default creator of upstream templates based on route headers.
/// </summary>
/// <remarks>Ocelot feature: Routing based on request header.</remarks>
public partial class UpstreamHeaderTemplatePatternCreator : IUpstreamHeaderTemplatePatternCreator
{
#if NET7_0_OR_GREATER
    [GeneratedRegex(@"(\{header:.*?\})", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
    private static partial Regex RegExPlaceholders();
#else
    private static readonly Regex RegExPlaceholdersVar = new(@"(\{header:.*?\})", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromMilliseconds(1000));
    private static Regex RegExPlaceholders() => RegExPlaceholdersVar;
#endif

    public Dictionary<string, UpstreamHeaderTemplate> Create(IRoute route)
    {
        var resultHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>();

        foreach (var headerTemplate in route.UpstreamHeaderTemplates)
        {
            var headerTemplateValue = headerTemplate.Value;
            var matches = RegExPlaceholders().Matches(headerTemplateValue);

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

            resultHeaderTemplates.Add(headerTemplate.Key, new(template, headerTemplate.Value));
        }

        return resultHeaderTemplates;
    }
}
