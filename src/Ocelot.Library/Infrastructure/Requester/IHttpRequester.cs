using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.Library.Infrastructure.RequestBuilder;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Requester
{
    public interface IHttpRequester
    {
        Task<Response<HttpResponseMessage>> GetResponse(Request request);
    }
}
