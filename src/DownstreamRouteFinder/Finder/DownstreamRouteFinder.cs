using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Middleware;
using Ocelot.Responses;
using HttpMethod = System.Net.Http.HttpMethod;

namespace Ocelot.DownstreamRouteFinder.Finder;

/// <summary>
/// Static route finder.
/// </summary>
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

    public Route Get(HttpRequest request, out IEnumerable<Error> errors)
    {
        var configuration = request.HttpContext.Items.IInternalConfiguration();
        var response = Find(configuration, request.Path, request.QueryString, new(request.Method), UpstreamHost(request), request.Headers);
        errors = response.Errors;
        return response.Data;
    }

    [Obsolete(Use_Get_HttpRequest_IEnumerable_method)]
    public Response<Route> Get(string upstreamUrlPath, string upstreamQueryString, string httpMethod,
        IInternalConfiguration configuration, string upstreamHost, IHeaderDictionary upstreamHeaders)
    {
        throw new NotSupportedException(Use_Get_HttpRequest_IEnumerable_method); // Aggressively ask developers to stop using it!
    }
    internal const string Use_Get_HttpRequest_IEnumerable_method = "Use the Get(HttpRequest, IEnumerable<Error>) method instead of this one! Note that this method has been deprecated since version 26.0!";

    protected virtual Response<Route> Find(IInternalConfiguration configuration,
        PathString upstreamUrlPath, QueryString upstreamQueryString, HttpMethod method, HostString upstreamHost, IHeaderDictionary upstreamHeaders)
    {
        var downstreamRoutes = new List<Route>();

        var applicableRoutes = configuration.Routes
            .Where(r => !r.IsDynamic && IsRouteApplicableToThisRequest(r, method, upstreamHost)) // process static routes only
            .OrderByDescending(x => x.UpstreamTemplatePattern.Priority);

        foreach (var route in applicableRoutes)
        {
            var urlMatch = _urlMatcher.Match(upstreamUrlPath, upstreamQueryString.ToString(), route.UpstreamTemplatePattern);
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
            return new ErrorResponse<Route>(new UnableToFindDownstreamRouteError(upstreamUrlPath, method));

        var notNullOption = downstreamRoutes.FirstOrDefault(x => !string.IsNullOrEmpty(x.UpstreamHost));
        var nullOption = downstreamRoutes.FirstOrDefault(x => string.IsNullOrEmpty(x.UpstreamHost));
        return new OkResponse<Route>(notNullOption ?? nullOption);
    }

    protected virtual bool IsRouteApplicableToThisRequest(Route route, HttpMethod method, HostString upstreamHost)
        => (route.UpstreamHttpMethod.Count == 0 || route.UpstreamHttpMethod.Contains(method))
                &&
           (route.UpstreamHost.IsEmpty() || route.UpstreamHost == upstreamHost.Host);

    /// <summary>
    /// Finds placeholder values via both URL and header processing for the route,
    /// finally returning a new instance of <see cref="Route"/> with an initialized <see cref="Route.TemplatePlaceholderNameAndValues"/> collection for safe consumption by scoped services.
    /// </summary>
    /// <param name="route">The current route.</param>
    /// <param name="path">The path part of the request URL.</param>
    /// <param name="query">The query string part of the request URL.</param>
    /// <param name="upstreamHeaders">The headers of the request.</param>
    /// <returns>A new <see cref="Route"/> instance to be consumed.</returns>
    protected virtual Route FindPlaceholders(Route route, string path, QueryString query, IHeaderDictionary upstreamHeaders)
    {
        var placeholders = _pathPlaceholderFinder.Find(path, query.ToString(), route.UpstreamTemplatePattern.OriginalValue)
            .Data; // TODO Adjust the interface. Lol!
        var headerPlaceholders = _headerPlaceholderFinder.Find(upstreamHeaders, route.UpstreamHeaderTemplates);
        placeholders.AddRange(headerPlaceholders);
        return new Route(route, placeholders);
    }

    protected virtual HostString UpstreamHost(HttpRequest request) => request.Host;
}
