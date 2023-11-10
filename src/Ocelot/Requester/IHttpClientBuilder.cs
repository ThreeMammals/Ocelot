using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;

namespace Ocelot.Requester
{
    public interface IHttpClientBuilder
    {
        IHttpClient Create(DownstreamRoute downstreamRoute, HttpContext httpContext);

        void Save();
    }
}
