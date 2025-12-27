using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Values;

namespace Ocelot.DownstreamUrlCreator;

/// <summary>
/// TODO Move this service to the middleware as a protected virtual method. Having a separate interface is absolutely useless.
/// </summary>
public class DownstreamPathPlaceholderReplacer : IDownstreamPathPlaceholderReplacer
{
    public DownstreamPath Replace(string downstreamPathTemplate, List<PlaceholderNameAndValue> urlPathPlaceholderNameAndValues)
    {
        var downstreamPath = new StringBuilder(downstreamPathTemplate);
        foreach (var placeholderVariableAndValue in urlPathPlaceholderNameAndValues)
        {
            downstreamPath.Replace(placeholderVariableAndValue.Name, placeholderVariableAndValue.Value);
        }

        return new(downstreamPath.ToString());
    }
}
