using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Values;

namespace Ocelot.DownstreamRouteFinder.HeaderMatcher;

public class HeaderPlaceholderNameAndValueFinder : IHeaderPlaceholderNameAndValueFinder
{
    public IList<PlaceholderNameAndValue> Find(IDictionary<string, string> upstreamHeaders, IDictionary<string, UpstreamHeaderTemplate> templateHeaders)
    {
        var result = new List<PlaceholderNameAndValue>();
        foreach (var templateHeader in templateHeaders)
        {
            var upstreamHeader = upstreamHeaders[templateHeader.Key];
            var matches = templateHeader.Value.Pattern.Matches(upstreamHeader);
            var placeholders = matches
                .SelectMany(g => g.Groups as IEnumerable<Group>)
                .Where(g => g.Name != "0")
                .Select(g => new PlaceholderNameAndValue(string.Concat('{', g.Name, '}'), g.Value));
            result.AddRange(placeholders);
        }

        return result;
    }
}
