using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public sealed class GlobalRateLimitOptionsCreator : IGlobalRateLimitOptionsCreator
{
    public IEnumerable<GlobalRateLimitOptions> Create(FileGlobalConfiguration global)
    {
        var groups = global.GlobalRateLimitRules
            .Select(g => new GlobalRateLimitOptions
            {
                Name = g.Name,
                Pattern = new Regex("^" + Regex.Escape(g.Pattern).Replace("\\*", ".*") + "$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                Methods = g.Methods, // new HashSet<string>(g.Methods, StringComparer.OrdinalIgnoreCase),
                Limit = g.Limit,
                Period = g.Period,
                HttpStatusCode = g.HttpStatusCode,
                QuotaExceededMessage = g.QuotaExceededMessage,
                DisableRateLimitHeaders = g.DisableRateLimitHeaders,
                EnableRateLimiting = g.EnableRateLimiting,
            })
            .ToArray();

        return groups;
    }
}
