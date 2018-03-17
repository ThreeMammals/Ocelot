using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.Requester
{
    public interface IHttpRequester
    {
        Task<Response<HttpResponseMessage>> GetResponse(DownstreamContext context);
    }
}
