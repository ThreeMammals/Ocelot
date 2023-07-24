using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Request.Mapper
{
    public interface IRequestMapper
    {
        Task<Response<HttpRequestMessage>> Map(HttpRequest request, DownstreamRoute downstreamRoute);
    }
}
