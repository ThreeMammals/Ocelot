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
        IUrlPathToUrlTemplateMatcher urlMatcher,
        IPlaceholderNameAndValueFinder pathPlaceholderFinder,
        IHeadersToHeaderTemplatesMatcher headerMatcher,
        IHeaderPlaceholderNameAndValueFinder headerPlaceholderFinder)
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
                var updated = GetPlaceholderNamesAndValues(upstreamUrlPath, upstreamQueryString, route, upstreamHeaders);
                downstreamRoutes.Add(updated);
            }
        }

        if (downstreamRoutes.Count != 0)
        {
            var notNullOption = downstreamRoutes.FirstOrDefault(x => !string.IsNullOrEmpty(x.UpstreamHost));
            var nullOption = downstreamRoutes.FirstOrDefault(x => string.IsNullOrEmpty(x.UpstreamHost));
            return new OkResponse<Route>(notNullOption ?? nullOption);
        }

        return new ErrorResponse<Route>(new UnableToFindDownstreamRouteError(upstreamUrlPath, httpMethod));
    }

    private static bool RouteIsApplicableToThisRequest(Route route, string httpMethod, string upstreamHost)
    {
        var method = new HttpMethod(httpMethod.Trim());
        return (route.UpstreamHttpMethod.Count == 0 || route.UpstreamHttpMethod.Contains(method))
                &&
               (string.IsNullOrEmpty(route.UpstreamHost) || route.UpstreamHost == upstreamHost);
    }

    private Route GetPlaceholderNamesAndValues(string path, string query, Route route, IHeaderDictionary upstreamHeaders)
    {
        var placeholders = _pathPlaceholderFinder.Find(path, query, route.UpstreamTemplatePattern.OriginalValue)
            .Data; // TODO Adjust the interface. Lol!
        var headerPlaceholders = _headerPlaceholderFinder.Find(upstreamHeaders, route.UpstreamHeaderTemplates);
        placeholders.AddRange(headerPlaceholders);
        route.TemplatePlaceholderNameAndValues.AddRange(placeholders); // No double object creation. Awesome!
        return route;
    }
}
