using System.Collections.Generic;
using Ocelot.Library.Responses;

namespace Ocelot.Library.DownstreamRouteFinder.UrlMatcher
{
    public interface ITemplateVariableNameAndValueFinder
    {
        Response<List<TemplateVariableNameAndValue>> Find(string upstreamUrlPath, string upstreamUrlPathTemplate);
    }
}
