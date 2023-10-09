using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;
using Ocelot.Responses;
using System.Buffers;

namespace Ocelot.Request.Mapper;

public class RequestMapper : IRequestMapper
{
    private readonly string[] _unsupportedHeaders = { "host" };
    private const int DefaultBufferSize = 65536;

    public async Task<Response<HttpRequestMessage>> Map(HttpRequest request, DownstreamRoute downstreamRoute)
    {
        try
        {
            var requestMessage = new HttpRequestMessage
            {
                Content = await MapContent(request),
                Method = MapMethod(request, downstreamRoute),
                RequestUri = MapUri(request),
                Version = downstreamRoute.DownstreamHttpVersion,
            };

            MapHeaders(request, requestMessage);

            return new OkResponse<HttpRequestMessage>(requestMessage);
        }
        catch (Exception ex)
        {
            return new ErrorResponse<HttpRequestMessage>(new UnmappableRequestError(ex));
        }
    }

    private static async Task<HttpContent> MapContent(HttpRequest request)
    {
        if (request.Body is { CanSeek: true, Length: <= 0 })
        {
            return null;
        }

        // Never change this to StreamContent again, I forgot it doesnt work in #464.
        var content = await CopyAsync(request.Body, CancellationToken.None);

        if (!string.IsNullOrEmpty(request.ContentType))
        {
            content.Headers
                .TryAddWithoutValidation("Content-Type", new[] { request.ContentType });
        }

        AddHeaderIfExistsOnRequest("Content-Language", content, request);
        AddHeaderIfExistsOnRequest("Content-Location", content, request);
        AddHeaderIfExistsOnRequest("Content-Range", content, request);
        AddHeaderIfExistsOnRequest("Content-MD5", content, request);
        AddHeaderIfExistsOnRequest("Content-Disposition", content, request);
        AddHeaderIfExistsOnRequest("Content-Encoding", content, request);

        return content;
    }

    private static void AddHeaderIfExistsOnRequest(string key, HttpContent content, HttpRequest request)
    {
        if (request.Headers.ContainsKey(key))
        {
            content.Headers
                .TryAddWithoutValidation(key, request.Headers[key].ToArray());
        }
    }

    private static HttpMethod MapMethod(HttpRequest request, DownstreamRoute downstreamRoute)
    {
        return !string.IsNullOrEmpty(downstreamRoute?.DownstreamHttpMethod) ? new HttpMethod(downstreamRoute.DownstreamHttpMethod) : new HttpMethod(request.Method);
    }

    private static Uri MapUri(HttpRequest request)
    {
        return new Uri(request.GetEncodedUrl());
    }

    private void MapHeaders(HttpRequest request, HttpRequestMessage requestMessage)
    {
        foreach (var header in request.Headers)
        {
            if (IsSupportedHeader(header))
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
    }

    private bool IsSupportedHeader(KeyValuePair<string, StringValues> header)
    {
        return !_unsupportedHeaders.Contains(header.Key.ToLower());
    }

    private static async Task<ByteArrayContent> CopyAsync(Stream input, CancellationToken cancellation)
    {
        if (input == null)
        {
            return null;
        }

        byte[] buffer = null;
        try
        {
            var inputLength = input.CanSeek ? (int)input.Length : DefaultBufferSize;
            buffer = ArrayPool<byte>.Shared.Rent(inputLength);
            var read = await input.ReadAsync(buffer.AsMemory(), cancellation);
            return read == 0 ? null : new ByteArrayContent(buffer.AsMemory(0, read).ToArray());
        }
        finally
        {
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
