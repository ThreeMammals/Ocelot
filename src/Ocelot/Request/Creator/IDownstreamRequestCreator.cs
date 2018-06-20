namespace Ocelot.Request.Creator
{
    using System.Net.Http;
    using Ocelot.Request.Middleware;

    public interface IDownstreamRequestCreator
    {
        DownstreamRequest Create(HttpRequestMessage request);
    }
}
