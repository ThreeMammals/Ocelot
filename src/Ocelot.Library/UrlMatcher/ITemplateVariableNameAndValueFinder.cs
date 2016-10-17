namespace Ocelot.Library.UrlMatcher
{
    using System.Collections.Generic;
    using Responses;

    public interface ITemplateVariableNameAndValueFinder
    {
        Response<List<TemplateVariableNameAndValue>> Find(string upstreamUrlPath, string upstreamUrlPathTemplate);
    }
}
