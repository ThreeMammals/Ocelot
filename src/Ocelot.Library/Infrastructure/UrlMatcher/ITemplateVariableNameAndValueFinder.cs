using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlMatcher
{
    public interface ITemplateVariableNameAndValueFinder
    {
        Response<List<TemplateVariableNameAndValue>> Find(string upstreamUrlPath, string upstreamUrlPathTemplate);
    }
}
