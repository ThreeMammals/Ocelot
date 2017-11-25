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
        private readonly IUrlPathPlaceholderNameAndValueFinder _urlPathPlaceholderNameAndValueFinder;

        public DownstreamRouteFinder(IUrlPathToUrlTemplateMatcher urlMatcher, IUrlPathPlaceholderNameAndValueFinder urlPathPlaceholderNameAndValueFinder)
        {
            _urlMatcher = urlMatcher;
            _urlPathPlaceholderNameAndValueFinder = urlPathPlaceholderNameAndValueFinder;
        }

        public Response<DownstreamRoute> FindDownstreamRoute(string upstreamUrlPath, string upstreamHttpMethod, IOcelotConfiguration configuration)
        {
            var applicableReRoutes = configuration.ReRoutes.Where(r => r.UpstreamHttpMethod.Count == 0 || r.UpstreamHttpMethod.Select(x => x.Method.ToLower()).Contains(upstreamHttpMethod.ToLower()));

            foreach (var reRoute in applicableReRoutes)
            {
                if (upstreamUrlPath == reRoute.UpstreamTemplatePattern)
                {
                    var templateVariableNameAndValues = _urlPathPlaceholderNameAndValueFinder.Find(upstreamUrlPath, reRoute.UpstreamPathTemplate.Value);

                    return new OkResponse<DownstreamRoute>(new DownstreamRoute(templateVariableNameAndValues.Data, reRoute));
                }

                var urlMatch = _urlMatcher.Match(upstreamUrlPath, reRoute.UpstreamTemplatePattern);

                if (urlMatch.Data.Match)
                {
                    var templateVariableNameAndValues = _urlPathPlaceholderNameAndValueFinder.Find(upstreamUrlPath, reRoute.UpstreamPathTemplate.Value);

                    return new OkResponse<DownstreamRoute>(new DownstreamRoute(templateVariableNameAndValues.Data, reRoute));
                }
            }
        
            return new ErrorResponse<DownstreamRoute>(new List<Error>
            {
                new UnableToFindDownstreamRouteError()
            });
        }
    }
}