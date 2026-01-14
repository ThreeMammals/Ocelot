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
    [GeneratedRegex(@"(\{header:.*?\})", RegexOptions.IgnoreCase | RegexOptions.Singleline, RegexGlobal.DefaultMatchTimeoutMilliseconds, "en-US")]
    private static partial Regex RegexPlaceholders();

    public IDictionary<string, UpstreamHeaderTemplate> Create(IDictionary<string, string> upstreamHeaderTemplates, bool routeIsCaseSensitive)
    {
        var result = new Dictionary<string, UpstreamHeaderTemplate>();

        foreach (var headerTemplate in upstreamHeaderTemplates)
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

            var template = routeIsCaseSensitive
                ? $"^{headerTemplateValue}$"
                : $"^(?i){headerTemplateValue}$"; // ignore case

            result.Add(headerTemplate.Key, new(template, headerTemplate.Value));
        }

        return result;
    }

    public IDictionary<string, UpstreamHeaderTemplate> Create(IRouteUpstream route)
        => Create(route.UpstreamHeaderTemplates, route.RouteIsCaseSensitive);
}
