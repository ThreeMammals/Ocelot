using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Ocelot.Library.Infrastructure.RequestBuilder
{
    public class RequestBuilder : IRequestBuilder
    {
        public Request Build(string httpMethod, string downstreamUrl, Stream content, IHeaderDictionary headers,
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
                var splitCt = contentType.Split(';');
                var cT = splitCt[0];
                httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(cT);
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
                    httpRequestMessage.Headers.Add(header.Key, header.Value.ToArray());
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


            return new Request(httpRequestMessage, cookieContainer);
        }
    }
}