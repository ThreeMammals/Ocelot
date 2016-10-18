using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.RequestBuilder;
using Ocelot.Responses;

namespace Ocelot.Requester
{
    public interface IHttpRequester
    {
        Task<Response<HttpResponseMessage>> GetResponse(Request request);
    }
}
