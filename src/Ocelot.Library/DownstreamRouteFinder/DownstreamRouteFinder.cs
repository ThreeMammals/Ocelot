using System;
using System.Collections.Generic;
using System.Linq;
using Ocelot.Library.Configuration.Provider;
using Ocelot.Library.Errors;
using Ocelot.Library.Responses;
using Ocelot.Library.UrlMatcher;

namespace Ocelot.Library.DownstreamRouteFinder
{
    public class DownstreamRouteFinder : IDownstreamRouteFinder
    {
        private readonly IOcelotConfigurationProvider _configProvider;
        private readonly IUrlPathToUrlTemplateMatcher _urlMatcher;
        private readonly ITemplateVariableNameAndValueFinder _templateVariableNameAndValueFinder;

        public DownstreamRouteFinder(IOcelotConfigurationProvider configProvider, IUrlPathToUrlTemplateMatcher urlMatcher, ITemplateVariableNameAndValueFinder templateVariableNameAndValueFinder)
        {
            _configProvider = configProvider;
            _urlMatcher = urlMatcher;
            _templateVariableNameAndValueFinder = templateVariableNameAndValueFinder;
        }

        public Response<DownstreamRoute> FindDownstreamRoute(string upstreamUrlPath, string upstreamHttpMethod)
        {
            var configuration = _configProvider.Get();

            foreach (var template in configuration.Data.ReRoutes.Where(r => string.Equals(r.UpstreamHttpMethod, upstreamHttpMethod, StringComparison.CurrentCultureIgnoreCase)))
            {
                var urlMatch = _urlMatcher.Match(upstreamUrlPath, template.UpstreamTemplatePattern);

                if (urlMatch.Data.Match)
                {
                    var templateVariableNameAndValues = _templateVariableNameAndValueFinder.Find(upstreamUrlPath,
                        template.UpstreamTemplate);

                    return new OkResponse<DownstreamRoute>(new DownstreamRoute(templateVariableNameAndValues.Data, template));
                }
            }
        
            return new ErrorResponse<DownstreamRoute>(new List<Error>
            {
                new UnableToFindDownstreamRouteError()
            });
        }
    }
}