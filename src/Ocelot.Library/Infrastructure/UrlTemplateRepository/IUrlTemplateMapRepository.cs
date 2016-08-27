using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlTemplateRepository
{
    public interface IUrlTemplateMapRepository
    {
        Response AddUrlTemplateMap(UrlTemplateMap urlPathMap);
        Response<List<UrlTemplateMap>> All { get; }
    }
}   