using System.Text.RegularExpressions;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher
{
    public class RegExUrlMatcher : IUrlPathToUrlTemplateMatcher
    {
        public Response<UrlMatch> Match(string upstreamUrlPath, string upstreamQueryString, string upstreamUrlPathTemplate, bool containsQueryString)
        {
            var regex = new Regex(upstreamUrlPathTemplate);

            if (!containsQueryString)
            {
                return regex.IsMatch(upstreamUrlPath)
                    ? new OkResponse<UrlMatch>(new UrlMatch(true))
                    : new OkResponse<UrlMatch>(new UrlMatch(false));
            }

            return regex.IsMatch($"{upstreamUrlPath}{upstreamQueryString}") 
                ? new OkResponse<UrlMatch>(new UrlMatch(true)) 
                : new OkResponse<UrlMatch>(new UrlMatch(false));
        }
    }
}
