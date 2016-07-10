using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlFinder
{
    public interface IUpstreamBaseUrlFinder
    {
        Response<string> FindUpstreamBaseUrl(string downstreamBaseUrl);
    }
}