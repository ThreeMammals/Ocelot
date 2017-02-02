using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public interface IDownstreamRouteFinder
    {
        Response<DownstreamRoute> FindDownstreamRoute(string upstreamUrlPath, string upstreamHttpMethod);
    }
}
