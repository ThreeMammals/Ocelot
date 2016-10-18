using System.Collections.Generic;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher
{
    public interface ITemplateVariableNameAndValueFinder
    {
        Response<List<TemplateVariableNameAndValue>> Find(string upstreamUrlPath, string upstreamUrlPathTemplate);
    }
}
