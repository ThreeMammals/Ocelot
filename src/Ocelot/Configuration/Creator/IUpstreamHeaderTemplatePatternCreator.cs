using Ocelot.Configuration.File;
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
    /// <returns>A <see cref="Dictionary{TKey, TValue}"/> object where TKey is <see langword="string"/>, TValue is <see cref="UpstreamHeaderTemplate"/>.</returns>
    Dictionary<string, UpstreamHeaderTemplate> Create(IRoute route);
}
