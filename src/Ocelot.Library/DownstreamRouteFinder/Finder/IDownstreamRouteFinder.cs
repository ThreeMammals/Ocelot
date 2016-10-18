using Ocelot.Library.Responses;

namespace Ocelot.Library.DownstreamRouteFinder.Finder
{
    public interface IDownstreamRouteFinder
    {
        Response<DownstreamRoute> FindDownstreamRoute(string upstreamUrlPath, string upstreamHttpMethod);
    }
}
