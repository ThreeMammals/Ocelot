namespace Ocelot.Request.Creator
{
    using System.Net.Http;

    using Middleware;

    public interface IDownstreamRequestCreator
    {
        DownstreamRequest Create(HttpRequestMessage request);
    }
}
