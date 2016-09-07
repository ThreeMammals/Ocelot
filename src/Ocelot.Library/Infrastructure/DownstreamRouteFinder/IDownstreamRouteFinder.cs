using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.DownstreamRouteFinder
{
    public interface IDownstreamRouteFinder
    {
        Response<DownstreamRoute> FindDownstreamRoute(string upstreamUrlPath);
    }
}
