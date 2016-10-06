namespace Ocelot.Library.Infrastructure.RequestBuilder
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.AspNetCore.Http;

    public class RequestBuilder : IRequestBuilder
    {
        public HttpRequestMessage Build(string httpMethod, string downstreamUrl, Stream content, IHeaderDictionary headers,
            IRequestCookieCollection cookies, IQueryCollection queryString, string contentType)
        {
            var method = new HttpMethod(httpMethod);

            var uri = new Uri(downstreamUrl + queryString);

            var httpRequestMessage = new HttpRequestMessage(method, uri)
            {
                Content = new StreamContent(content),
            };

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

            return httpRequestMessage;

        }
    }
}