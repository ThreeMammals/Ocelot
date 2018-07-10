using System.Text.RegularExpressions;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher
{
    public class RegExUrlMatcher : IUrlPathToUrlTemplateMatcher
    {
        public Response<UrlMatch> Match(string upstreamUrlPath, string upstreamUrlQueryString, string upstreamUrlPathTemplate)
        {
            var regex = new Regex(upstreamUrlPathTemplate);

            return regex.IsMatch($"{upstreamUrlPath}{upstreamUrlQueryString}") 
                ? new OkResponse<UrlMatch>(new UrlMatch(true)) 
                : new OkResponse<UrlMatch>(new UrlMatch(false));
        }
    }
}
