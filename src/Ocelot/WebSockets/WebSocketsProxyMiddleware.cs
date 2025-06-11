// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Modified https://github.com/aspnet/Proxy websockets class to use in Ocelot.

using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using System.Net.WebSockets;

namespace Ocelot.WebSockets;

public class WebSocketsProxyMiddleware : OcelotMiddleware
{
    public static readonly string[] NotForwardedWebSocketHeaders = new[]
    {
        "Connection", "Host", "Upgrade",
        "Sec-WebSocket-Accept", "Sec-WebSocket-Protocol", "Sec-WebSocket-Key", "Sec-WebSocket-Version", "Sec-WebSocket-Extensions",
    };

    private const int DefaultWebSocketBufferSize = 4096;
    private readonly RequestDelegate _next;
    private readonly IWebSocketsFactory _factory;

    public const string IgnoredSslWarningFormat = $"You have ignored all SSL warnings by using {nameof(DownstreamRoute.DangerousAcceptAnyServerCertificateValidator)} for this downstream route! {nameof(DownstreamRoute.UpstreamPathTemplate)}: '{{0}}', {nameof(DownstreamRoute.DownstreamPathTemplate)}: '{{1}}'.";
    public const string InvalidSchemeWarningFormat = "Invalid scheme has detected which will be replaced! Scheme '{0}' of the downstream '{1}'.";

    public WebSocketsProxyMiddleware(IOcelotLoggerFactory logging,
        RequestDelegate next,
        IWebSocketsFactory factory)
        : base(logging.CreateLogger<WebSocketsProxyMiddleware>())
    {
        _next = next;
        _factory = factory;
    }

    public async Task Invoke(HttpContext context)
    {
        var request = context.Items.DownstreamRequest();
        var route = context.Items.DownstreamRoute();
        await Proxy(context, request, route);
    }

    protected virtual async Task PumpAsync(WebSocket source, WebSocket destination, int bufferSize, CancellationToken cancellation)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);
        var buffer = new byte[bufferSize];
        while (true)
        {
            WebSocketReceiveResult result = default;
            try
            {
                result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), cancellation);
            }
            catch (OperationCanceledException)
            {
                await TryCloseOutputAsync(destination, WebSocketCloseStatus.EndpointUnavailable, nameof(OperationCanceledException), cancellation);
                //await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, nameof(OperationCanceledException), cancellation);
                return; // we don't rethrow timeout/cancellation errors
            }
            catch (WebSocketException e)
            {
                if (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                {
                    await TryCloseOutputAsync(destination, WebSocketCloseStatus.EndpointUnavailable, $"{nameof(WebSocketException)} when {nameof(e.WebSocketErrorCode)} is {nameof(WebSocketError.ConnectionClosedPrematurely)}", cancellation);
                    //await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, $"{nameof(WebSocketException)} when {nameof(e.WebSocketErrorCode)} is {nameof(WebSocketError.ConnectionClosedPrematurely)}", cancellation);
                }

                // DON'T THROW, NEVER! Just log the warning...
                // The logging level has been decreased from level 4 (Error) to level 3 (Warning) due to the high number of disconnecting events for sensitive WebSocket connections in unstable networks.
                Logger.LogWarning(() => $"{nameof(WebSocketException)} when {nameof(e.WebSocketErrorCode)} is {e.WebSocketErrorCode}");
                return; // swallow the error
                //throw;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await TryCloseOutputAsync(destination, source.CloseStatus.Value, source.CloseStatusDescription, cancellation);
                //await destination.CloseOutputAsync(source.CloseStatus.Value, source.CloseStatusDescription, cancellation);
                return;
            }

            if (destination.State == WebSocketState.Open)
            {
                await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, cancellation);
            }
        }
    }

    private async Task Proxy(HttpContext context, DownstreamRequest request, DownstreamRoute route)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(route);

        if (!context.WebSockets.IsWebSocketRequest)
        {
            throw new InvalidOperationException();
        }

        var client = _factory.CreateClient(); // new ClientWebSocket();
        if (route.DangerousAcceptAnyServerCertificateValidator)
        {
            client.Options.RemoteCertificateValidationCallback = (request, certificate, chain, errors) => true;
            Logger.LogWarning(() => string.Format(IgnoredSslWarningFormat, route.UpstreamPathTemplate, route.DownstreamPathTemplate));
        }

        foreach (var protocol in context.WebSockets.WebSocketRequestedProtocols)
        {
            client.Options.AddSubProtocol(protocol);
        }

        foreach (var header in context.Request.Headers)
        {
            if (!NotForwardedWebSocketHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    client.Options.SetRequestHeader(header.Key, header.Value);
                }
                catch (ArgumentException)
                {
                    // Expected in .NET Framework for headers that are mistakenly considered restricted.
                    // See: https://github.com/dotnet/corefx/issues/26627
                    // .NET Core does not exhibit this issue, ironically due to a separate bug (https://github.com/dotnet/corefx/issues/18784)
                }
            }
        }

        // Only Uris starting with 'ws://' or 'wss://' are supported in System.Net.WebSockets.ClientWebSocket
        var scheme = request.Scheme;
        if (!scheme.StartsWith(Uri.UriSchemeWs))
        {
            Logger.LogWarning(() => string.Format(InvalidSchemeWarningFormat, scheme, request.ToUri()));
            request.Scheme = scheme == Uri.UriSchemeHttp
                ? Uri.UriSchemeWs
                : scheme == Uri.UriSchemeHttps ? Uri.UriSchemeWss : scheme;
        }

        var destinationUri = new Uri(request.ToUri());
        await client.ConnectAsync(destinationUri, context.RequestAborted);

        using var server = await context.WebSockets.AcceptWebSocketAsync(client.SubProtocol);
        await Task.WhenAll(
            PumpAsync(client.ToWebSocket(), server, DefaultWebSocketBufferSize, context.RequestAborted),
            PumpAsync(server, client.ToWebSocket(), DefaultWebSocketBufferSize, context.RequestAborted));
    }

    /// <summary>
    /// Closes the WebSocket only if its state is <see cref="WebSocketState.Open"/> or <see cref="WebSocketState.CloseReceived"/>.
    /// </summary>
    /// <returns>The underlying closing task if the <paramref name="webSocket"/> <see cref="WebSocket.State"/> matches; otherwise, the <see cref="Task.CompletedTask"/>.</returns>
    protected virtual Task TryCloseOutputAsync(WebSocket webSocket, WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellation)
        => (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
            ? webSocket.CloseOutputAsync(closeStatus, statusDescription, cancellation)
            : Task.CompletedTask;
}
