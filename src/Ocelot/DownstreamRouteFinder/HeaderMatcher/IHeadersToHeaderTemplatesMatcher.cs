﻿using Ocelot.Values;

namespace Ocelot.DownstreamRouteFinder.HeaderMatcher;

/// <summary>
/// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/routing.rst#upstream-headers">Routing based on request header</see>.
/// </summary>
public interface IHeadersToHeaderTemplatesMatcher
{
    bool Match(Dictionary<string, string> upstreamHeaders, Dictionary<string, UpstreamHeaderTemplate> routeHeaders);
}
