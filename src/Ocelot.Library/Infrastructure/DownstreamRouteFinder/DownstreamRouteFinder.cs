using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Ocelot.Library.Infrastructure.Configuration;
using Ocelot.Library.Infrastructure.Errors;
using Ocelot.Library.Infrastructure.Responses;
using Ocelot.Library.Infrastructure.UrlMatcher;

namespace Ocelot.Library.Infrastructure.DownstreamRouteFinder
{
    public class DownstreamRouteFinder : IDownstreamRouteFinder
    {
        private readonly IOcelotConfiguration _configuration;
        private readonly IUrlPathToUrlTemplateMatcher _urlMatcher;
        private readonly ITemplateVariableNameAndValueFinder _templateVariableNameAndValueFinder;

        public DownstreamRouteFinder(IOcelotConfiguration configuration, IUrlPathToUrlTemplateMatcher urlMatcher, ITemplateVariableNameAndValueFinder templateVariableNameAndValueFinder)
        {
            _configuration = configuration;
            _urlMatcher = urlMatcher;
            _templateVariableNameAndValueFinder = templateVariableNameAndValueFinder;
        }

        public Response<DownstreamRoute> FindDownstreamRoute(string upstreamUrlPath, string upstreamHttpMethod)
        {
            foreach (var template in _configuration.ReRoutes.Where(r => string.Equals(r.UpstreamHttpMethod, upstreamHttpMethod, StringComparison.CurrentCultureIgnoreCase)))
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