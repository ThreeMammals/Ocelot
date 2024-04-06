using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.Creator;
using Ocelot.DownstreamRouteFinder.HeaderMatcher;

namespace Ocelot.DependencyInjection;

public static class Features
{
    /// <summary>
    /// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/routing.rst#upstream-headers">Routing based on request header</see>.
    /// </summary>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddHeaderRouting(this IServiceCollection services) => services
        .AddSingleton<IUpstreamHeaderTemplatePatternCreator, UpstreamHeaderTemplatePatternCreator>()
        .AddSingleton<IHeadersToHeaderTemplatesMatcher, HeadersToHeaderTemplatesMatcher>()
        .AddSingleton<IHeaderPlaceholderNameAndValueFinder, HeaderPlaceholderNameAndValueFinder>();
}
