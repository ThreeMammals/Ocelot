using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlFinder
{
    public interface IUpstreamHostUrlFinder
    {
        Response<string> FindUpstreamHostUrl(string downstreamHostUrl);
    }
}
