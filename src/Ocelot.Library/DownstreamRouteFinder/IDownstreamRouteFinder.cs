namespace Ocelot.Library.DownstreamRouteFinder
{
    using Responses;

    public interface IDownstreamRouteFinder
    {
        Response<DownstreamRoute> FindDownstreamRoute(string upstreamUrlPath, string upstreamHttpMethod);
    }
}
