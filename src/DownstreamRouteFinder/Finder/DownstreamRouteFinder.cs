using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.Finder;

public class DownstreamRouteFinder : IDownstreamRouteProvider
{
    private readonly IUrlPathToUrlTemplateMatcher _urlMatcher;
    private readonly IPlaceholderNameAndValueFinder _pathPlaceholderFinder;
    private readonly IHeadersToHeaderTemplatesMatcher _headerMatcher;
    private readonly IHeaderPlaceholderNameAndValueFinder _headerPlaceholderFinder;

    public DownstreamRouteFinder(
        IUrlPathToUrlTemplateMatcher urlMatcher, IPlaceholderNameAndValueFinder pathPlaceholderFinder,
        IHeadersToHeaderTemplatesMatcher headerMatcher, IHeaderPlaceholderNameAndValueFinder headerPlaceholderFinder)
    {
        _urlMatcher = urlMatcher;
        _pathPlaceholderFinder = pathPlaceholderFinder;
        _headerMatcher = headerMatcher;
        _headerPlaceholderFinder = headerPlaceholderFinder;
    }

    public Response<Route> Get(string upstreamUrlPath, string upstreamQueryString, string httpMethod,
        IInternalConfiguration configuration, string upstreamHost, IHeaderDictionary upstreamHeaders)
    {
        var downstreamRoutes = new List<Route>();

        var applicableRoutes = configuration.Routes
            .Where(r => !r.IsDynamic && RouteIsApplicableToThisRequest(r, httpMethod, upstreamHost)) // process static routes only
            .OrderByDescending(x => x.UpstreamTemplatePattern.Priority);

        foreach (var route in applicableRoutes)
        {
            var urlMatch = _urlMatcher.Match(upstreamUrlPath, upstreamQueryString, route.UpstreamTemplatePattern);
            var headersMatch = _headerMatcher.Match(upstreamHeaders, route.UpstreamHeaderTemplates);
            if (urlMatch.Match && headersMatch)
            {
                var newRoute = FindPlaceholders(route, upstreamUrlPath, upstreamQueryString, upstreamHeaders);
                downstreamRoutes.Add(newRoute);
            }
        }

        // Something abnormal has happened, so return shortly.
        // TODO: Add headers info to the error
        if (downstreamRoutes.Count == 0)
            return new ErrorResponse<Route>(new UnableToFindDownstreamRouteError(upstreamUrlPath, httpMethod));

        var notNullOption = downstreamRoutes.FirstOrDefault(x => !string.IsNullOrEmpty(x.UpstreamHost));
        var nullOption = downstreamRoutes.FirstOrDefault(x => string.IsNullOrEmpty(x.UpstreamHost));
        return new OkResponse<Route>(notNullOption ?? nullOption);
    }

    private static bool RouteIsApplicableToThisRequest(Route route, string httpMethod, string upstreamHost)
    {
        var method = new HttpMethod(httpMethod.Trim());
        return (route.UpstreamHttpMethod.Count == 0 || route.UpstreamHttpMethod.Contains(method))
                &&
               (string.IsNullOrEmpty(route.UpstreamHost) || route.UpstreamHost == upstreamHost);
    }

    /// <summary>
    /// Finds placeholder values via both URL and header processing for the route,
    /// finally returning a new instance of <see cref="Route"/> with an initialized <see cref="Route.TemplatePlaceholderNameAndValues"/> collection for safe consumption by scoped services.
    /// </summary>
    /// <param name="route">The current route.</param>
    /// <param name="path">The path part of the request URL.</param>
    /// <param name="query">The query string part of the request URL.</param>
    /// <param name="upstreamHeaders">The headers of the request.</param>
    /// <returns>A new <see cref="Route"/> instance to be consumed.</returns>
    protected virtual Route FindPlaceholders(Route route, string path, string query, IHeaderDictionary upstreamHeaders)
    {
        var placeholders = _pathPlaceholderFinder.Find(path, query, route.UpstreamTemplatePattern.OriginalValue)
            .Data; // TODO Adjust the interface. Lol!
        var headerPlaceholders = _headerPlaceholderFinder.Find(upstreamHeaders, route.UpstreamHeaderTemplates);
        placeholders.AddRange(headerPlaceholders);
        return new Route(route, placeholders);
    }
}
