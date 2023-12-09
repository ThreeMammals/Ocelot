using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;

namespace Ocelot.Request.Mapper;

public class RequestMapper : IRequestMapper
{
    private readonly HashSet<string> _unsupportedHeaders = new(StringComparer.OrdinalIgnoreCase) { "host" };
    private readonly string[] _contentHeaders = { "Content-Length", "Content-Language", "Content-Location", "Content-Range", "Content-MD5", "Content-Disposition", "Content-Encoding" };

    public HttpRequestMessage Map(HttpRequest request, DownstreamRoute downstreamRoute)
    {
        var requestMessage = new HttpRequestMessage
        {
            Content = MapContent(request),
            Method = MapMethod(request, downstreamRoute),
            RequestUri = MapUri(request),
            Version = downstreamRoute.DownstreamHttpVersion,
        };

        MapHeaders(request, requestMessage);

        return requestMessage;
    }

    private HttpContent MapContent(HttpRequest request)
    {
        // todo: We should check if we really need to call HttpRequest.Body.Length
        // but we assume that if CanSeek is true, the length is calculated without
        // an important overhead
        if (request.Body is null or { CanSeek: true, Length: <= 0 })
        {
            return null;
        }

        var content = new StreamHttpContent(request.HttpContext);

        AddContentHeaders(request, content);

        return content;
    }

    private void AddContentHeaders(HttpRequest request, HttpContent content)
    {
        if (!string.IsNullOrEmpty(request.ContentType))
        {
            content.Headers
                .TryAddWithoutValidation("Content-Type", new[] { request.ContentType });
        }

        // The performance might be improved by retrieving the matching headers from the request
        // instead of calling request.Headers.TryGetValue for each used content header
        var matchingHeaders = _contentHeaders.Where(header => request.Headers.ContainsKey(header));

        foreach (var key in matchingHeaders)
        {
            if (!request.Headers.TryGetValue(key, out var value))
            {
                continue;
            }

            content.Headers.TryAddWithoutValidation(key, value.ToArray());
        }
    }

    private static HttpMethod MapMethod(HttpRequest request, DownstreamRoute downstreamRoute) => 
        !string.IsNullOrEmpty(downstreamRoute?.DownstreamHttpMethod) ? 
            new HttpMethod(downstreamRoute.DownstreamHttpMethod) : new HttpMethod(request.Method);

    // todo: review this method, request.GetEncodedUrl() could throw a NullReferenceException
    private static Uri MapUri(HttpRequest request) => new(request.GetEncodedUrl());

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

    private bool IsSupportedHeader(KeyValuePair<string, StringValues> header) =>
        !_unsupportedHeaders.Contains(header.Key);
}
