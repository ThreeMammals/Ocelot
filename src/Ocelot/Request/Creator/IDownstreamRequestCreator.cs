using Ocelot.Request.Middleware;

namespace Ocelot.Request.Creator
{
    public interface IDownstreamRequestCreator
    {
        DownstreamRequest Create(HttpRequestMessage request);
    }
}
