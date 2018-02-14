using System.Collections.Generic;
using System.Linq;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public class DownstreamRouteFinder : IDownstreamRouteFinder
    {
        private readonly IUrlPathToUrlTemplateMatcher _urlMatcher;
        private readonly IPlaceholderNameAndValueFinder _placeholderNameAndValueFinder;

        public DownstreamRouteFinder(IUrlPathToUrlTemplateMatcher urlMatcher, IPlaceholderNameAndValueFinder urlPathPlaceholderNameAndValueFinder)
        {
            _urlMatcher = urlMatcher;
            _placeholderNameAndValueFinder = urlPathPlaceholderNameAndValueFinder;
        }

        public Response<DownstreamRoute> FindDownstreamRoute(string path, string httpMethod, IOcelotConfiguration configuration, string upstreamHost)
        {
            var downstreamRoutes = new List<DownstreamRoute>();

            var applicableReRoutes = configuration.ReRoutes
                .Where(r => RouteIsApplicableToThisRequest(r, httpMethod, upstreamHost))
                .OrderByDescending(x => x.UpstreamTemplatePattern.Priority);

            foreach (var reRoute in applicableReRoutes)
            {
                var urlMatch = _urlMatcher.Match(path, reRoute.UpstreamTemplatePattern.Template);

                if (urlMatch.Data.Match)
                {
                    downstreamRoutes.Add(GetPlaceholderNamesAndValues(path, reRoute));
                }
            }

            if (downstreamRoutes.Any())
            {
                var notNullOption = downstreamRoutes.FirstOrDefault(x => !string.IsNullOrEmpty(x.ReRoute.UpstreamHost));
                var nullOption = downstreamRoutes.FirstOrDefault(x => string.IsNullOrEmpty(x.ReRoute.UpstreamHost));

                return notNullOption != null ? new OkResponse<DownstreamRoute>(notNullOption) : new OkResponse<DownstreamRoute>(nullOption);
            }

            return new ErrorResponse<DownstreamRoute>(new List<Error>
            {
                new UnableToFindDownstreamRouteError()
            });
        }

        private bool RouteIsApplicableToThisRequest(ReRoute reRoute, string httpMethod, string upstreamHost)
        {
            return reRoute.UpstreamHttpMethod.Count == 0 || reRoute.UpstreamHttpMethod.Select(x => x.Method.ToLower()).Contains(httpMethod.ToLower()) && !(!string.IsNullOrEmpty(reRoute.UpstreamHost) && reRoute.UpstreamHost != upstreamHost);
        }

        private DownstreamRoute GetPlaceholderNamesAndValues(string path, ReRoute reRoute)
        {
            var templatePlaceholderNameAndValues = _placeholderNameAndValueFinder.Find(path, reRoute.UpstreamPathTemplate.Value);

            return new DownstreamRoute(templatePlaceholderNameAndValues.Data, reRoute);
        }
    }
}
