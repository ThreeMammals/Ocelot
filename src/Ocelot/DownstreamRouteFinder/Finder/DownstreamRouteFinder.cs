using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public class DownstreamRouteFinder : IDownstreamRouteProvider
    {
        private readonly IUrlPathToUrlTemplateMatcher _urlMatcher;
        private readonly IPlaceholderNameAndValueFinder _placeholderNameAndValueFinder;

        public DownstreamRouteFinder(IUrlPathToUrlTemplateMatcher urlMatcher, IPlaceholderNameAndValueFinder urlPathPlaceholderNameAndValueFinder)
        {
            _urlMatcher = urlMatcher;
            _placeholderNameAndValueFinder = urlPathPlaceholderNameAndValueFinder;
        }

        public Response<DownstreamRoute> Get(
            string upstreamUrlPath,
            string upstreamQueryString,
            string httpMethod,
            IInternalConfiguration configuration,
            string upstreamHost,
            IHeaderDictionary requestHeaders)
        {
            var downstreamRoutes = new List<DownstreamRoute>();

            var applicableReRoutes = configuration.ReRoutes
                .Where(r => RouteIsApplicableToThisRequest(r, httpMethod, upstreamHost, requestHeaders))
                .OrderByDescending(x => x.UpstreamTemplatePattern.Priority);

            foreach (var reRoute in applicableReRoutes)
            {
                var urlMatch = _urlMatcher.Match(upstreamUrlPath, upstreamQueryString, reRoute.UpstreamTemplatePattern);

                if (urlMatch.Data.Match)
                {
                    downstreamRoutes.Add(GetPlaceholderNamesAndValues(upstreamUrlPath, upstreamQueryString, reRoute));
                }
            }

            if (downstreamRoutes.Any())
            {
                var notNullOption = downstreamRoutes.FirstOrDefault(x => !string.IsNullOrEmpty(x.ReRoute.UpstreamHost));
                var nullOption = downstreamRoutes.FirstOrDefault(x => string.IsNullOrEmpty(x.ReRoute.UpstreamHost));

                return notNullOption != null ? new OkResponse<DownstreamRoute>(notNullOption) : new OkResponse<DownstreamRoute>(nullOption);
            }

            return new ErrorResponse<DownstreamRoute>(new UnableToFindDownstreamRouteError(upstreamUrlPath, httpMethod));
        }

        private bool RouteIsApplicableToThisRequest(ReRoute reRoute, string httpMethod, string upstreamHost, IHeaderDictionary requestHeaders)
        {
            return (reRoute.UpstreamHttpMethod.Count == 0 || RouteHasHttpMethod(reRoute, httpMethod)) &&
                   (string.IsNullOrEmpty(reRoute.UpstreamHost) || reRoute.UpstreamHost == upstreamHost) &&
                   (reRoute.UpstreamHeaderRoutingOptions == null || !reRoute.UpstreamHeaderRoutingOptions.Enabled() || RouteHasRequiredUpstreamHeaders(reRoute, requestHeaders));
        }

        private bool RouteHasHttpMethod(ReRoute reRoute, string httpMethod)
        {
            return reRoute.UpstreamHttpMethod.Select(x => x.Method.ToLower()).Contains(httpMethod.ToLower());
        }

        private bool RouteHasRequiredUpstreamHeaders(ReRoute reRoute, IHeaderDictionary requestHeaders)
        {
            switch (reRoute.UpstreamHeaderRoutingOptions.Mode)
            {
                case UpstreamHeaderRoutingCombinationMode.Any:
                    return reRoute.UpstreamHeaderRoutingOptions.Headers.HasAnyOf(requestHeaders);
                case UpstreamHeaderRoutingCombinationMode.All:
                    return reRoute.UpstreamHeaderRoutingOptions.Headers.HasAllOf(requestHeaders);
            }

            return false;
        }

        private DownstreamRoute GetPlaceholderNamesAndValues(string path, string query, ReRoute reRoute)
        {
            var templatePlaceholderNameAndValues = _placeholderNameAndValueFinder.Find(path, query, reRoute.UpstreamTemplatePattern.OriginalValue);

            return new DownstreamRoute(templatePlaceholderNameAndValues.Data, reRoute);
        }
    }
}
