using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;

namespace Ocelot.Library.Infrastructure.Requester
{
    public class HttpClientHttpRequester : IHttpRequester
    {
        public async Task<HttpResponseMessage> GetResponse(string httpMethod, string downstreamUrl, Stream content = null)
        {
            var method = new HttpMethod(httpMethod);

            return await downstreamUrl.SendAsync(method, new StreamContent(content));
        }
    }
}