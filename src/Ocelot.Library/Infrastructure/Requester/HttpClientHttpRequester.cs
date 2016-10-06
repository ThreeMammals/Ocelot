using System.Net.Http;
using System.Threading.Tasks;

namespace Ocelot.Library.Infrastructure.Requester
{
    using HttpClient;

    public class HttpClientHttpRequester : IHttpRequester
    {
        private readonly IHttpClient _httpClient;

        public HttpClientHttpRequester(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> GetResponse(HttpRequestMessage httpRequestMessage)
        {
            return await _httpClient.SendAsync(httpRequestMessage);
        }
    }
}