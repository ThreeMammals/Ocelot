namespace Ocelot.Request.Mapper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.Extensions.Primitives;
    using Ocelot.Responses;

    public class RequestMapper : IRequestMapper
    {
        private readonly string[] _unsupportedHeaders = { "host" };

        public async Task<Response<HttpRequestMessage>> Map(HttpRequest request)
        {
            try
            {
                var requestMessage = new HttpRequestMessage()
                {
                    Content = await MapContent(request),
                    Method = MapMethod(request),
                    RequestUri = MapUri(request)
                };

                MapHeaders(request, requestMessage);

                return new OkResponse<HttpRequestMessage>(requestMessage);
            }
            catch (Exception ex)
            {
                return new ErrorResponse<HttpRequestMessage>(new UnmappableRequestError(ex));
            }
        }

        private async Task<HttpContent> MapContent(HttpRequest request)
        {
            if (request.Body == null)
            {
                return null;
            }

            var content = new ByteArrayContent(await ToByteArray(request.Body));

            content.Headers.TryAddWithoutValidation("Content-Type", new[] {request.ContentType});

            return content;
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

