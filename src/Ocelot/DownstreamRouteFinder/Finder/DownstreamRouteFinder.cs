using System;
using System.Collections.Generic;
using System.Linq;
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

        public Response<DownstreamRoute> FindDownstreamRoute(string upstreamUrlPath, string upstreamHttpMethod)
        {
            var configuration = _configProvider.Get();

            var applicableReRoutes = configuration.Data.ReRoutes.Where(r => string.Equals(r.UpstreamHttpMethod.Method, upstreamHttpMethod, StringComparison.CurrentCultureIgnoreCase));

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