using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Ocelot.Headers;
using Ocelot.Middleware;

namespace Ocelot.Responder;

/// <summary>
/// Cannot unit test things in this class due to methods not being implemented on .NET concretes used for testing.
/// </summary>
public class HttpContextResponder : IHttpResponder
{
    private readonly IRemoveOutputHeaders _removeOutputHeaders;

    public HttpContextResponder(IRemoveOutputHeaders removeOutputHeaders)
    {
        _removeOutputHeaders = removeOutputHeaders;
    }

    public async Task SetResponseOnHttpContext(HttpContext context, DownstreamResponse response)
    {
        _removeOutputHeaders.Remove(response.Headers);

        foreach (var httpResponseHeader in response.Headers)
        {
            AddHeaderIfDoesntExist(context, httpResponseHeader);
        }

        SetStatusCode(context, (int)response.StatusCode);

        context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = response.ReasonPhrase;

        // As of 5.0 HttpResponse.Content never returns null.
        // https://github.com/dotnet/runtime/blame/8fc68f626a11d646109a758cb0fc70a0aa7826f1/src/libraries/System.Net.Http/src/System/Net/Http/HttpResponseMessage.cs#L46
        // TODO: Check if it applies to ocelot custom implementation
        if (response.Content is null)
        {
            return;
        }

        foreach (var httpResponseHeader in response.Content.Headers)
        {
            AddHeaderIfDoesntExist(context, new Header(httpResponseHeader.Key, httpResponseHeader.Value));
        }

        if (response.Content.Headers.ContentLength != null)
        {
            AddHeaderIfDoesntExist(context,
                new Header("Content-Length", new[] { response.Content.Headers.ContentLength.ToString() }));
        }

        if (response.StatusCode != HttpStatusCode.NotModified && context.Response.ContentLength != 0)
        {
            await using var content = await response.Content.ReadAsStreamAsync();
            await content.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
    }

    public void SetErrorResponseOnContext(HttpContext context, int statusCode)
    {
        SetStatusCode(context, statusCode);
    }

    public async Task SetErrorResponseOnContext(HttpContext context, DownstreamResponse response)
    {
        if (response.Content.Headers.ContentLength != null)
        {
            AddHeaderIfDoesntExist(context,
                new Header("Content-Length", new[] { response.Content.Headers.ContentLength.ToString() }));
        }

        if (context.Response.ContentLength != 0)
        {
            await using var content = await response.Content.ReadAsStreamAsync();
            await content.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
    }

    private static void SetStatusCode(HttpContext context, int statusCode)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = statusCode;
        }
    }

    private static void AddHeaderIfDoesntExist(HttpContext context, Header httpResponseHeader)
    {
        if (!context.Response.Headers.ContainsKey(httpResponseHeader.Key))
        {
            context.Response.Headers.Append(httpResponseHeader.Key,
                new StringValues(httpResponseHeader.Values.ToArray()));
        }
    }
}
