using Microsoft.AspNetCore.Http;
using Ocelot.Responses;

namespace Ocelot.Requester
{
    public interface IHttpRequester
    {
        Task<Response<HttpResponseMessage>> GetResponse(HttpContext httpContext);
    }
}
