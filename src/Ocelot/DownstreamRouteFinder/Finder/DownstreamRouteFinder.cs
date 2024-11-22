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

    public Response<DownstreamRouteHolder> Get(string upstreamUrlPath, string upstreamQueryString, string httpMethod,
        IInternalConfiguration configuration, string upstreamHost, IDictionary<string, string> upstreamHeaders)
    {
        var downstreamRoutes = new List<DownstreamRouteHolder>();

        var applicableRoutes = configuration.Routes
            .Where(r => RouteIsApplicableToThisRequest(r, httpMethod, upstreamHost))
            .OrderByDescending(x => x.UpstreamTemplatePattern.Priority);

        foreach (var route in applicableRoutes)
        {
            var urlMatch = _urlMatcher.Match(upstreamUrlPath, upstreamQueryString, route.UpstreamTemplatePattern);
            var headersMatch = _headerMatcher.Match(upstreamHeaders, route.UpstreamHeaderTemplates);

            if (urlMatch.Data.Match && headersMatch)
            {
                downstreamRoutes.Add(GetPlaceholderNamesAndValues(upstreamUrlPath, upstreamQueryString, route, upstreamHeaders));
            }
        }

        if (downstreamRoutes.Count != 0)
        {
            var notNullOption = downstreamRoutes.FirstOrDefault(x => !string.IsNullOrEmpty(x.Route.UpstreamHost));
            var nullOption = downstreamRoutes.FirstOrDefault(x => string.IsNullOrEmpty(x.Route.UpstreamHost));
            return new OkResponse<DownstreamRouteHolder>(notNullOption ?? nullOption);
        }

        return new ErrorResponse<DownstreamRouteHolder>(new UnableToFindDownstreamRouteError(upstreamUrlPath, httpMethod));
    }

    private static bool RouteIsApplicableToThisRequest(Route route, string httpMethod, string upstreamHost)
    {
        return (route.UpstreamHttpMethod.Count == 0 || route.UpstreamHttpMethod.Select(x => x.Method.ToLower()).Contains(httpMethod.ToLower())) &&
               (string.IsNullOrEmpty(route.UpstreamHost) || route.UpstreamHost == upstreamHost);
    }

    private DownstreamRouteHolder GetPlaceholderNamesAndValues(string path, string query, Route route, IDictionary<string, string> upstreamHeaders)
    {
        var templatePlaceholderNameAndValues = _pathPlaceholderFinder
            .Find(path, query, route.UpstreamTemplatePattern.OriginalValue)
            .Data;
        var headerPlaceholders = _headerPlaceholderFinder.Find(upstreamHeaders, route.UpstreamHeaderTemplates);
        templatePlaceholderNameAndValues.AddRange(headerPlaceholders);

        return new DownstreamRouteHolder(templatePlaceholderNameAndValues, route);
    }
}
