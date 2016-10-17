namespace Ocelot.Library.DownstreamRouteFinder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Configuration;
    using Errors;
    using Responses;
    using UrlMatcher;

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