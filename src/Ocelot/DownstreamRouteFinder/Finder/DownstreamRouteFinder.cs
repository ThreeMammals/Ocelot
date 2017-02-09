using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Configuration.Provider;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public class DownstreamRouteFinder : IDownstreamRouteFinder
    {
        private readonly IOcelotConfigurationProvider _configProvider;
        private readonly IUrlPathToUrlTemplateMatcher _urlMatcher;
        private readonly IUrlPathPlaceholderNameAndValueFinder _urlPathPlaceholderNameAndValueFinder;

        public DownstreamRouteFinder(IOcelotConfigurationProvider configProvider, IUrlPathToUrlTemplateMatcher urlMatcher, IUrlPathPlaceholderNameAndValueFinder urlPathPlaceholderNameAndValueFinder)
        {
            _configProvider = configProvider;
            _urlMatcher = urlMatcher;
            _urlPathPlaceholderNameAndValueFinder = urlPathPlaceholderNameAndValueFinder;
        }

        public async Task<Response<DownstreamRoute>> FindDownstreamRoute(string upstreamUrlPath, string upstreamHttpMethod)
        {
            var configuration = await _configProvider.Get();

            var applicableReRoutes = configuration.Data.ReRoutes.Where(r => string.Equals(r.UpstreamHttpMethod, upstreamHttpMethod, StringComparison.CurrentCultureIgnoreCase));

            foreach (var reRoute in applicableReRoutes)
            {
                var urlMatch = _urlMatcher.Match(upstreamUrlPath, reRoute.UpstreamTemplatePattern);

                if (urlMatch.Data.Match)
                {
                    var templateVariableNameAndValues = _urlPathPlaceholderNameAndValueFinder.Find(upstreamUrlPath, reRoute.UpstreamTemplate);

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