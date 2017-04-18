using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ocelot.Request
{
    public class Mapper
    {
        private readonly string[] _unsupportedHeaders = { "host" };

        public async Task<HttpRequestMessage> Map(HttpRequest request)
        {
            var requestMessage = new HttpRequestMessage()
            {
                Content = await MapContent(request),
                Method = MapMethod(request),
                RequestUri = MapUri(request),
                //Properties = null
                //Version = null
            };

            MapHeaders(request, requestMessage);

            return requestMessage;
        }

        private async Task<HttpContent> MapContent(HttpRequest request)
        {
            if (request.Body == null)
            {
                return null;
            }


            return new ByteArrayContent(await ToByteArray(request.Body));
        }

        private HttpMethod MapMethod(HttpRequest request)
        {
            return new HttpMethod(request.Method);
        }

        private Uri MapUri(HttpRequest request)
        {
            return new Uri(request.GetEncodedUrl());
        }

        private void MapHeaders(HttpRequest request, HttpRequestMessage requestMessage)
        {
            foreach (var header in request.Headers)
            {
                //todo get rid of if..
                if (IsSupportedHeader(header))
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
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

        private bool IsSupportedHeader(KeyValuePair<string, StringValues> header)
        {
            return !_unsupportedHeaders.Contains(header.Key.ToLower());
        }
    }
}

