using Ocelot.Request.Middleware;
using System.Net.Http;

namespace Ocelot.Request.Creator
{
    public interface IDownstreamRequestCreator
    {
        DownstreamRequest Create(HttpRequestMessage request);
    }
}
