using System.Net.Http;
using System.Threading.Tasks;

namespace Ocelot.Library.Infrastructure.Requester
{
    public interface IHttpRequester
    {
        Task<HttpResponseMessage> GetResponse(HttpRequestMessage httpRequestMessage);
    }
}
