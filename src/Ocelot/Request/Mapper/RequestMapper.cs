using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;

namespace Ocelot.Request.Mapper;

public class RequestMapper : IRequestMapper
{
    private static readonly HashSet<string> UnsupportedHeaders = new(StringComparer.OrdinalIgnoreCase) { "host", "transfer-encoding" };
    private static readonly string[] ContentHeaders = { "Content-Length", "Content-Language", "Content-Location", "Content-Range", "Content-MD5", "Content-Disposition", "Content-Encoding" };

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

    private static HttpContent MapContent(HttpRequest request)
    {
        HttpContent content;

        // No content if we have no body or if the request has no content according to RFC 2616 section 4.3
        if (request.Body == null
            || (!request.ContentLength.HasValue && StringValues.IsNullOrEmpty(request.Headers.TransferEncoding)))
        {
            return null;
        }

        content = request.ContentLength is 0
            ? new ByteArrayContent(Array.Empty<byte>()) 
            : new StreamHttpContent(request.HttpContext);

        AddContentHeaders(request, content);

        return content;
    }

    private static void AddContentHeaders(HttpRequest request, HttpContent content)
    {
        if (!string.IsNullOrEmpty(request.ContentType))
        {
            content.Headers
                .TryAddWithoutValidation("Content-Type", new[] { request.ContentType });
        }

        // The performance might be improved by retrieving the matching headers from the request
        // instead of calling request.Headers.TryGetValue for each used content header
        var matchingHeaders = ContentHeaders.Where(header => request.Headers.ContainsKey(header));

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

    // TODO Review this method, request.GetEncodedUrl() could throw a NullReferenceException
    private static Uri MapUri(HttpRequest request) => new(request.GetEncodedUrl());

    private static void MapHeaders(HttpRequest request, HttpRequestMessage requestMessage)
    {
        foreach (var header in request.Headers)
        {
            if (IsSupportedHeader(header))
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
    }

    private static bool IsSupportedHeader(KeyValuePair<string, StringValues> header) =>
        !UnsupportedHeaders.Contains(header.Key);
}
