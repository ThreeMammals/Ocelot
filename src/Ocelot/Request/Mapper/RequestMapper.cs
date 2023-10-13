using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Request.Mapper;

public class RequestMapper : IRequestMapper
{
    private readonly string[] _unsupportedHeaders = { "host" };
    

    public Task<Response<HttpRequestMessage>> Map(HttpRequest request, DownstreamRoute downstreamRoute)
    {
        try
        {
            var requestMessage = new HttpRequestMessage
            {
                Content = MapContent(request),
                Method = MapMethod(request, downstreamRoute),
                RequestUri = MapUri(request),
                Version = downstreamRoute.DownstreamHttpVersion,
            };

            MapHeaders(request, requestMessage);

            return Task.FromResult<Response<HttpRequestMessage>>(new OkResponse<HttpRequestMessage>(requestMessage));
        }
        catch (Exception ex)
        {
            return Task.FromResult<Response<HttpRequestMessage>>(new ErrorResponse<HttpRequestMessage>(new UnmappableRequestError(ex)));
        }
    }

    private static HttpContent MapContent(HttpRequest request)
    {
        if (request.Body is null or { CanSeek: true, Length: <= 0 })
        {
            return null;
        }

        var content = new StreamHttpContent(request.HttpContext);

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

    private static async Task<ByteArrayContent> CopyAsync(Stream stream)
    {
        if (stream == null)
        {
            return null;
        }

        await using (stream)
        {
            using var memStream = new MemoryStream();
            await stream.CopyToAsync(memStream);
            return new ByteArrayContent(memStream.ToArray());
        }
    }
}
