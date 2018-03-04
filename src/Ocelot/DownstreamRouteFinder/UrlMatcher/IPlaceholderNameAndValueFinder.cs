using System.Collections.Generic;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher
{
    public interface IPlaceholderNameAndValueFinder
    {
        Response<List<PlaceholderNameAndValue>> Find(string path, string pathTemplate);
    }
}
