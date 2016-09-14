using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Http;

namespace Ocelot.Library.Infrastructure.Requester
{
    public class HttpClientHttpRequester : IHttpRequester
    {
        public async Task<HttpResponseMessage> GetResponse(
            string httpMethod, 
            string downstreamUrl, 
            Stream content,
            IHeaderDictionary headers,
            IRequestCookieCollection cookies,
            IQueryCollection queryString)
        {
            var method = new HttpMethod(httpMethod);
            var streamContent = new StreamContent(content);

            if (content.Length > 0)
            {
                return await downstreamUrl
                .SetQueryParams(queryString)
                .WithCookies(cookies)
                .WithHeaders(streamContent.Headers)
                .SendAsync(method, streamContent);
            }

            return await downstreamUrl
                .SetQueryParams(queryString)
                .WithHeaders(headers)
                .WithCookies(cookies)
                .SendAsync(method, streamContent);
        }
    }
}