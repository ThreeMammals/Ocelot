using System.Net.Http;
using System.Threading.Tasks;

namespace Ocelot.Library.Infrastructure.Requester
{
    public class HttpClientHttpRequester : IHttpRequester
    {
        public async Task<HttpResponseMessage> GetResponse(string httpMethod, string downstreamUrl)
        {
            var method = new HttpMethod(httpMethod);

            var httpRequestMessage = new HttpRequestMessage(method, downstreamUrl);

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.SendAsync(httpRequestMessage);
                return response;
            }
        }
    }
}