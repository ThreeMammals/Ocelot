using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.BaseUrlRepository
{
    public interface IBaseUrlMapRepository
    {
        Response AddBaseUrlMap(BaseUrlMap baseUrlMap);
        Response<BaseUrlMap> GetBaseUrlMap(string downstreamUrl);
    }
} 