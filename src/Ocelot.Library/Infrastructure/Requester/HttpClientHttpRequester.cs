using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.Library.Infrastructure.RequestBuilder;

namespace Ocelot.Library.Infrastructure.Requester
{
    public class HttpClientHttpRequester : IHttpRequester
    {
        public async Task<HttpResponseMessage> GetResponse(Request request)
        {
            using (var handler = new HttpClientHandler { CookieContainer = request.CookieContainer })
            using (var httpClient = new HttpClient(handler))
            {
                return await httpClient.SendAsync(request.HttpRequestMessage);
            }
        }
    }
}