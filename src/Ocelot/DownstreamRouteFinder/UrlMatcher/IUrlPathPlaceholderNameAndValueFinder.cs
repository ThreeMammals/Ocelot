using System.Collections.Generic;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher
{
    public interface IUrlPathPlaceholderNameAndValueFinder
    {
        Response<List<UrlPathPlaceholderNameAndValue>> Find(string upstreamUrlPath, string upstreamUrlPathTemplate);
    }
}
