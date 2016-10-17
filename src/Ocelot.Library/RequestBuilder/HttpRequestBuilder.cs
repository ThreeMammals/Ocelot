namespace Ocelot.Library.RequestBuilder
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Responses;

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
                using (var reader = new StreamReader(content))
                {
                    var body = await reader.ReadToEndAsync();
                    httpRequestMessage.Content = new StringContent(body);
                }
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
                    //todo get rid of if..
                    if (header.Key.ToLower() != "host")
                    {
                        httpRequestMessage.Headers.Add(header.Key, header.Value.ToArray());
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