﻿using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public class DownstreamRouteFinder : IDownstreamRouteProvider
    {
        private readonly IUrlPathToUrlTemplateMatcher _urlMatcher;
        private readonly IPlaceholderNameAndValueFinder _placeholderNameAndValueFinder;
        private readonly IHeadersToHeaderTemplatesMatcher _headersMatcher;
        private readonly IHeaderPlaceholderNameAndValueFinder _headerPlaceholderNameAndValueFinder;

        public DownstreamRouteFinder(
            IUrlPathToUrlTemplateMatcher urlMatcher,
            IPlaceholderNameAndValueFinder urlPathPlaceholderNameAndValueFinder,
            IHeadersToHeaderTemplatesMatcher headersMatcher,
            IHeaderPlaceholderNameAndValueFinder headerPlaceholderNameAndValueFinder
            )
        {
            _urlMatcher = urlMatcher;
            _placeholderNameAndValueFinder = urlPathPlaceholderNameAndValueFinder;
            _headersMatcher = headersMatcher;
            _headerPlaceholderNameAndValueFinder = headerPlaceholderNameAndValueFinder;
        }

        public Response<DownstreamRouteHolder> Get(string upstreamUrlPath, string upstreamQueryString, string httpMethod,
            IInternalConfiguration configuration, string upstreamHost, Dictionary<string, string> upstreamHeaders)
        {
            var downstreamRoutes = new List<DownstreamRouteHolder>();

            var applicableRoutes = configuration.Routes
                .Where(r => RouteIsApplicableToThisRequest(r, httpMethod, upstreamHost))
                .OrderByDescending(x => x.UpstreamTemplatePattern.Priority);

            foreach (var route in applicableRoutes)
            {
                var urlMatch = _urlMatcher.Match(upstreamUrlPath, upstreamQueryString, route.UpstreamTemplatePattern);
                var headersMatch = _headersMatcher.Match(upstreamHeaders, route.UpstreamHeaderTemplates);

                if (urlMatch.Data.Match && headersMatch)
                {
                    downstreamRoutes.Add(GetPlaceholderNamesAndValues(upstreamUrlPath, upstreamQueryString, route, upstreamHeaders));
                }
            }

            if (downstreamRoutes.Any())
            {
                var notNullOption = downstreamRoutes.FirstOrDefault(x => !string.IsNullOrEmpty(x.Route.UpstreamHost));
                var nullOption = downstreamRoutes.FirstOrDefault(x => string.IsNullOrEmpty(x.Route.UpstreamHost));

                return notNullOption != null ? new OkResponse<DownstreamRouteHolder>(notNullOption) : new OkResponse<DownstreamRouteHolder>(nullOption);
            }

            return new ErrorResponse<DownstreamRouteHolder>(new UnableToFindDownstreamRouteError(upstreamUrlPath, httpMethod));
        }

        private static bool RouteIsApplicableToThisRequest(Route route, string httpMethod, string upstreamHost)
        {
            return (route.UpstreamHttpMethod.Count == 0 || route.UpstreamHttpMethod.Select(x => x.Method.ToLower()).Contains(httpMethod.ToLower())) &&
                   (string.IsNullOrEmpty(route.UpstreamHost) || route.UpstreamHost == upstreamHost);
        }

        private DownstreamRouteHolder GetPlaceholderNamesAndValues(string path, string query, Route route, Dictionary<string, string> upstreamHeaders)
        {
            var templatePlaceholderNameAndValues = _placeholderNameAndValueFinder.Find(path, query, route.UpstreamTemplatePattern.OriginalValue).Data;
            var headerPlaceholders = _headerPlaceholderNameAndValueFinder.Find(upstreamHeaders, route.UpstreamHeaderTemplates);
            templatePlaceholderNameAndValues.AddRange(headerPlaceholders);

            return new DownstreamRouteHolder(templatePlaceholderNameAndValues, route);
        }
    }
}
