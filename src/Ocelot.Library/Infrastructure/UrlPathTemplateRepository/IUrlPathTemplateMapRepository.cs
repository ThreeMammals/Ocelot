using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlPathTemplateRepository
{
    public interface IUrlPathTemplateMapRepository
    {
        Response AddUrlPathTemplateMap(UrlPathTemplateMap urlPathMap);
        Response<UrlPathTemplateMap> GetUrlPathTemplateMap(string downstreamUrlPathTemplate);
        Response<List<UrlPathTemplateMap>> All { get; }
    }
}   