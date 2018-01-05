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
        private readonly IPlaceholderNameAndValueFinder __placeholderNameAndValueFinder;

        public DownstreamRouteFinder(IUrlPathToUrlTemplateMatcher urlMatcher, IPlaceholderNameAndValueFinder urlPathPlaceholderNameAndValueFinder)
        {
            _urlMatcher = urlMatcher;
            __placeholderNameAndValueFinder = urlPathPlaceholderNameAndValueFinder;
        }

        public Response<DownstreamRoute> FindDownstreamRoute(string path, string httpMethod, IOcelotConfiguration configuration)
        {
            var applicableReRoutes = configuration.ReRoutes.Where(r => r.UpstreamHttpMethod.Count == 0 || r.UpstreamHttpMethod.Select(x => x.Method.ToLower()).Contains(httpMethod.ToLower())).OrderByDescending(x => x.UpstreamTemplatePattern.Priority);

            foreach (var reRoute in applicableReRoutes)
            {
                if (path == reRoute.UpstreamTemplatePattern.Template)
                {
                    return GetPlaceholderNamesAndValues(path, reRoute);
                }

                var urlMatch = _urlMatcher.Match(path, reRoute.UpstreamTemplatePattern.Template);

                if (urlMatch.Data.Match)
                {
                    return GetPlaceholderNamesAndValues(path, reRoute);
                }
            }
        
            return new ErrorResponse<DownstreamRoute>(new List<Error>
            {
                new UnableToFindDownstreamRouteError()
            });
        }

        private OkResponse<DownstreamRoute> GetPlaceholderNamesAndValues(string path, ReRoute reRoute)
        {
            var templatePlaceholderNameAndValues = __placeholderNameAndValueFinder.Find(path, reRoute.UpstreamPathTemplate.Value);

            return new OkResponse<DownstreamRoute>(new DownstreamRoute(templatePlaceholderNameAndValues.Data, reRoute));
        }
    }
}