using System.Net.Http;
using Ocelot.Request.Middleware;
using Ocelot.Responses;

namespace Ocelot.Infrastructure
{
    public interface IPlaceholders
    {
        Response<string> Get(string key);
        Response<string> Get(string key, DownstreamRequest request);
    }
}