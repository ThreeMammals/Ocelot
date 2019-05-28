using Ocelot.Middleware;
using Ocelot.Responses;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ocelot.Requester
{
    public interface IHttpRequester
    {
        Task<Response<HttpResponseMessage>> GetResponse(DownstreamContext context);
    }
}
