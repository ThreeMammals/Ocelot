using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Ocelot.Library.Infrastructure.Responses;
using Ocelot.Library.Infrastructure.UrlMatcher;

namespace Ocelot.Library.Infrastructure.DownstreamRouteFinder
{
    public class DownstreamRouteFinder : IDownstreamRouteFinder
    {
        private readonly IOptions<Configuration.Configuration> _configuration;
        private readonly IUrlPathToUrlTemplateMatcher _urlMatcher;

        public DownstreamRouteFinder(IOptions<Configuration.Configuration> configuration, IUrlPathToUrlTemplateMatcher urlMatcher)
        {
            _configuration = configuration;
            _urlMatcher = urlMatcher;
        }

        public Response<DownstreamRoute> FindDownstreamRoute(string upstreamUrlPath)
        {
            foreach (var template in _configuration.Value.ReRoutes)
            {
                var urlMatch = _urlMatcher.Match(upstreamUrlPath, template.UpstreamTemplate);

                if (urlMatch.Match)
                {
                    return new OkResponse<DownstreamRoute>(new DownstreamRoute(urlMatch.TemplateVariableNameAndValues, template.DownstreamTemplate));
                }
            }
        
            return new ErrorResponse<DownstreamRoute>(new List<Error>
            {
                new UnableToFindDownstreamRouteError()
            });
        }
    }
}