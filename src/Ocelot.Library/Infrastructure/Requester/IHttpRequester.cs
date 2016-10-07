using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.Library.Infrastructure.RequestBuilder;

namespace Ocelot.Library.Infrastructure.Requester
{
    public interface IHttpRequester
    {
        Task<HttpResponseMessage> GetResponse(Request request);
    }
}
