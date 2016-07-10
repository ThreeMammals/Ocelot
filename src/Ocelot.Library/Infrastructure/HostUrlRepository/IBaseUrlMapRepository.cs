using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.HostUrlRepository
{
    public interface IHostUrlMapRepository
    {
        Response AddBaseUrlMap(HostUrlMap baseUrlMap);
        Response<HostUrlMap> GetBaseUrlMap(string downstreamUrl);
    }
} 