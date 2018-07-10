using System.Collections.Generic;
using System.Linq;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Responses;

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

        public Response<DownstreamRoute> Get(string path, string queryString, string httpMethod, IInternalConfiguration configuration, string upstreamHost)
        {
            var downstreamRoutes = new List<DownstreamRoute>();

            var applicableReRoutes = configuration.ReRoutes
                .Where(r => RouteIsApplicableToThisRequest(r, httpMethod, upstreamHost))
                .OrderByDescending(x => x.UpstreamTemplatePattern.Priority);

            foreach (var reRoute in applicableReRoutes)
            {
                var urlMatch = _urlMatcher.Match(path, queryString, reRoute.UpstreamTemplatePattern.Template);

                if (urlMatch.Data.Match)
                {
                    downstreamRoutes.Add(GetPlaceholderNamesAndValues(path, queryString, reRoute));
                }
            }

            if (downstreamRoutes.Any())
            {
                var noUpstreamHost = downstreamRoutes.FirstOrDefault(x => !string.IsNullOrEmpty(x.ReRoute.UpstreamHost));
                var hasUpstreamHost = downstreamRoutes.FirstOrDefault(x => string.IsNullOrEmpty(x.ReRoute.UpstreamHost));

                return noUpstreamHost != null 
                    ? new OkResponse<DownstreamRoute>(noUpstreamHost) 
                    : new OkResponse<DownstreamRoute>(hasUpstreamHost);
            }

            return new ErrorResponse<DownstreamRoute>(new UnableToFindDownstreamRouteError(path, httpMethod));
        }

        private bool RouteIsApplicableToThisRequest(ReRoute reRoute, string httpMethod, string upstreamHost)
        {
            return (reRoute.UpstreamHttpMethod.Count == 0 || reRoute.UpstreamHttpMethod.Select(x => x.Method.ToLower()).Contains(httpMethod.ToLower())) &&
                   (string.IsNullOrEmpty(reRoute.UpstreamHost) || reRoute.UpstreamHost == upstreamHost);
        }

        private DownstreamRoute GetPlaceholderNamesAndValues(string path, string queryString, ReRoute reRoute)
        {
            var templatePlaceholderNameAndValues = _placeholderNameAndValueFinder.Find(path, queryString, reRoute.UpstreamPathTemplate.Value);

            return new DownstreamRoute(templatePlaceholderNameAndValues.Data, reRoute);
        }
    }
}
