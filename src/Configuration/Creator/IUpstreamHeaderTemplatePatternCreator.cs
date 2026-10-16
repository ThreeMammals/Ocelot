using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Values;

namespace Ocelot.Configuration.Creator;

/// <summary>
/// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/routing.rst#upstream-headers">Routing based on request header</see>.
/// </summary>
public interface IUpstreamHeaderTemplatePatternCreator
{
    /// <summary>
    /// Creates upstream templates based on route headers.
    /// </summary>
    /// <param name="route">The route info.</param>
    /// <returns>An <see cref="IDictionary{TKey, TValue}"/> object where TKey is <see langword="string"/>, TValue is <see cref="UpstreamHeaderTemplate"/>.</returns>
    IDictionary<string, UpstreamHeaderTemplate> Create(IRouteUpstream route);

    /// <summary>
    /// TODO: The purpose of the method is to create an instance for dynamic routing.
    /// </summary>
    /// <remarks>Warning!<br/>
    /// Under development! The interface is not stable!
    /// </remarks>
    /// <returns>An instance to be used by the <see cref="DiscoveryDownstreamRouteFinder"/> class.</returns>
    IDictionary<string, UpstreamHeaderTemplate> Create(IHeaderDictionary upstreamHeaderTemplates, bool routeIsCaseSensitive);
}
