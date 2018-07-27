using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public class DownstreamRouteFinder : IDownstreamRouteProvider
    {
        private readonly IUrlPathToUrlTemplateMatcher _urlMatcher;
        private readonly IPlaceholderNameAndValueFinder _placeholderNameAndValueFinder;

        public DownstreamRouteFinder(IUrlPathToUrlTemplateMatcher urlMatcher, IPlaceholderNameAndValueFinder urlPathPlaceholderNameAndValueFinder)
        {
            _urlMatcher = urlMatcher;
            _placeholderNameAndValueFinder = urlPathPlaceholderNameAndValueFinder;
        }

        public async Task<Response<DownstreamRoute>> GetAsync(string upstreamUrlPath, string upstreamQueryString, string httpMethod, IInternalConfiguration configuration, string upstreamHost)
        {
            var downstreamRoutes = new List<DownstreamRoute>();

            var applicableReRoutes = configuration.ReRoutes
                .Where(r => RouteIsApplicableToThisRequest(r, httpMethod, upstreamHost))
                .OrderByDescending(x => x.UpstreamTemplatePattern.Priority);

            foreach (var reRoute in applicableReRoutes)
            {
                var urlMatch = _urlMatcher.Match(upstreamUrlPath, upstreamQueryString, reRoute.UpstreamTemplatePattern.Template, reRoute.UpstreamTemplatePattern.ContainsQueryString);

                if (urlMatch.Data.Match)
                {
                    downstreamRoutes.Add(GetPlaceholderNamesAndValues(upstreamUrlPath, upstreamQueryString, reRoute));
                }
            }

            if (downstreamRoutes.Any())
            {
                var notNullOption = downstreamRoutes.FirstOrDefault(x => !string.IsNullOrEmpty(x.ReRoute.UpstreamHost));
                var nullOption = downstreamRoutes.FirstOrDefault(x => string.IsNullOrEmpty(x.ReRoute.UpstreamHost));

                var result = notNullOption != null ? new OkResponse<DownstreamRoute>(notNullOption) : new OkResponse<DownstreamRoute>(nullOption);
                return await Task.FromResult(result);
            }

            return await Task.FromResult(new ErrorResponse<DownstreamRoute>(new UnableToFindDownstreamRouteError(upstreamUrlPath, httpMethod)));
        }

        private bool RouteIsApplicableToThisRequest(ReRoute reRoute, string httpMethod, string upstreamHost)
        {
            return (reRoute.UpstreamHttpMethod.Count == 0 || reRoute.UpstreamHttpMethod.Select(x => x.Method.ToLower()).Contains(httpMethod.ToLower())) &&
                   (string.IsNullOrEmpty(reRoute.UpstreamHost) || reRoute.UpstreamHost == upstreamHost);
        }

        private DownstreamRoute GetPlaceholderNamesAndValues(string path, string query, ReRoute reRoute)
        {
            var templatePlaceholderNameAndValues = _placeholderNameAndValueFinder.Find(path, query, reRoute.UpstreamPathTemplate.Value);

            return new DownstreamRoute(templatePlaceholderNameAndValues.Data, reRoute);
        }
    }
}
