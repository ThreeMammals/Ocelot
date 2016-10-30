using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;

namespace Ocelot.Request.Builder
{
    public class HttpRequestBuilder : IRequestBuilder
    {
        public async Task<Response<Request>> Build(
            string httpMethod, 
            string downstreamUrl, 
            Stream content, 
            IHeaderDictionary headers,
            IRequestCookieCollection cookies, 
            QueryString queryString, 
            string contentType, 
            RequestId.RequestId requestId)
        {
            var method = new HttpMethod(httpMethod);

            var uri = new Uri(string.Format("{0}{1}", downstreamUrl, queryString.ToUriComponent()));

            var httpRequestMessage = new HttpRequestMessage(method, uri);

            if (content != null)
            {
                httpRequestMessage.Content = new ByteArrayContent(await ToByteArray(content));
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

                foreach (var header in headers)
                {
                    //todo get rid of if..
                    if (header.Key.ToLower() != "host")
                    {
                        httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }
            }

            if (RequestKeyIsNotNull(requestId) && !RequestIdInHeaders(requestId, httpRequestMessage.Headers))
            {
                ForwardRequestIdToDownstreamService(requestId, httpRequestMessage);
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

        private void ForwardRequestIdToDownstreamService(RequestId.RequestId requestId, HttpRequestMessage httpRequestMessage)
        {
            httpRequestMessage.Headers.Add(requestId.RequestIdKey, requestId.RequestIdValue);
        }

        private bool RequestIdInHeaders(RequestId.RequestId requestId, HttpRequestHeaders headers)
        {
            IEnumerable<string> value;
            if (headers.TryGetValues(requestId.RequestIdKey, out value))
            {
                return true;
            }

            return false;
        }

        private bool RequestKeyIsNotNull(RequestId.RequestId requestId)
        {
            return !string.IsNullOrEmpty(requestId?.RequestIdKey) && !string.IsNullOrEmpty(requestId.RequestIdValue);
        }

        private async Task<byte[]> ToByteArray(Stream stream)
        {
            using (stream)
            {
                using (var memStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memStream);
                    return memStream.ToArray();
                }
            }
        }
    }
}