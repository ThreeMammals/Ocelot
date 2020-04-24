namespace Ocelot.Requester
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.Middleware;
    using Ocelot.Responses;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Ocelot.Configuration;

    public interface IHttpRequester
    {
        Task<Response<HttpResponseMessage>> GetResponse(IDownstreamContext context, HttpContext httpContex, DownstreamReRoute downstreamReRoute);
    }
}
