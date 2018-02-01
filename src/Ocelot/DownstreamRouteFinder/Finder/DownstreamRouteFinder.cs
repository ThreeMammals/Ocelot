using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.Configuration.Provider;
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

            var applicableReRoutes = configuration.ReRoutes.Where(r => r.UpstreamHttpMethod.Count == 0 || r.UpstreamHttpMethod.Select(x => x.Method.ToLower()).Contains(httpMethod.ToLower())).OrderByDescending(x => x.UpstreamTemplatePattern.Priority);

            foreach (var reRoute in applicableReRoutes)
            {
                if (!string.IsNullOrEmpty(reRoute.UpstreamHost) && reRoute.UpstreamHost != upstreamHost)
                {
                    continue;
                }

                if (path == reRoute.UpstreamTemplatePattern.Template)
                {
                    downstreamRoutes.Add(GetPlaceholderNamesAndValues(path, reRoute));
                }

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

        private DownstreamRoute GetPlaceholderNamesAndValues(string path, ReRoute reRoute)
        {
            var templatePlaceholderNameAndValues = _placeholderNameAndValueFinder.Find(path, reRoute.UpstreamPathTemplate.Value);

            return new DownstreamRoute(templatePlaceholderNameAndValues.Data, reRoute);
        }
    }
}
