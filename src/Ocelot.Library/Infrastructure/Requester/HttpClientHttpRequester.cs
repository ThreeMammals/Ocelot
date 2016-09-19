using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
            IQueryCollection queryString,
            string contentType)
        {
            var method = new HttpMethod(httpMethod);
            var streamContent = new StreamContent(content);

            if (!string.IsNullOrEmpty(contentType))
            {
                var splitCt = contentType.Split(';');
                var cT = splitCt[0];
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(cT);
            }

            if (headers != null)
            {
                headers.Remove("Content-Type");
            }

            if (content.Length > 0)
            {
                return await downstreamUrl
                .SetQueryParams(queryString)
                .WithCookies(cookies)
                .WithHeaders(headers)
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