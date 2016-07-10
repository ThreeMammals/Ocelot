using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlPathTemplateRepository
{
    public interface IUrlPathTemplateMapRepository
    {
        Response AddUrlPathTemplateMap(UrlPathTemplateMap urlPathMap);
        Response<UrlPathTemplateMap> GetUrlPathTemplateMap(string downstreamUrlPathTemplate);
    }
}   