using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;

namespace Ocelot.RequestBuilder.Builder
{
    public class HttpRequestBuilder : IRequestBuilder
    {
        public async Task<Response<Request>> Build(string httpMethod, string downstreamUrl, Stream content, IHeaderDictionary headers,
            IRequestCookieCollection cookies, string queryString, string contentType)
        {
            var method = new HttpMethod(httpMethod);

            var uri = new Uri(string.Format("{0}{1}", downstreamUrl, queryString));

            var httpRequestMessage = new HttpRequestMessage(method, uri);

            if (content != null)
            {
                httpRequestMessage.Content = new StreamContent(content);
            }

            if (!string.IsNullOrEmpty(contentType))
            {
                httpRequestMessage.Content.Headers.Remove("Content-Type");
                httpRequestMessage.Content.Headers.TryAddWithoutValidation("Content-Type", contentType); 
            }

            //todo get rid of if
            if (headers != null)
            {
                headers.Remove("Content-Type");
            }

            //todo get rid of if
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    //todo get rid of if..
                    if (header.Key.ToLower() != "host")
                    {
                        httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }
            }

            var cookieContainer = new CookieContainer();

            //todo get rid of if
            if (cookies != null)
            {
                foreach (var cookie in cookies)
                {
                    cookieContainer.Add(uri, new Cookie(cookie.Key, cookie.Value));
                }
            }
            
            return new OkResponse<Request>(new Request(httpRequestMessage, cookieContainer));
        }
    }
}