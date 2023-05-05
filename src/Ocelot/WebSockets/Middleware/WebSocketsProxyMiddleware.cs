// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Modified https://github.com/aspnet/Proxy websockets class to use in Ocelot.

namespace Ocelot.WebSockets.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;

    using Logging;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;

    using Ocelot.Middleware;

    public class WebSocketsProxyMiddleware : OcelotMiddleware
    {
        private static readonly string[] NotForwardedWebSocketHeaders = new[]
        {
            "Connection", "Host", "Upgrade", "Sec-WebSocket-Accept", "Sec-WebSocket-Protocol", "Sec-WebSocket-Key",
            "Sec-WebSocket-Version", "Sec-WebSocket-Extensions"
        };

        private const int DefaultWebSocketBufferSize = 4096;
        private readonly RequestDelegate _next;

        public WebSocketsProxyMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory)
            : base(loggerFactory.CreateLogger<WebSocketsProxyMiddleware>())
        {
            _next = next;
        }

        private static async Task PumpWebSocket(WebSocket source, WebSocket destination, int bufferSize,
            CancellationToken cancellationToken)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            var buffer = new byte[bufferSize];
            while (true)
            {
                var (succeeded, result) = await TryReceiveAsync(source, destination, buffer, cancellationToken);
                if (!succeeded)
                {
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await destination.CloseOutputAsync(source.CloseStatus.Value, source.CloseStatusDescription,
                        cancellationToken);
                    return;
                }

                await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType,
                    result.EndOfMessage, cancellationToken);
            }
        }

        private static async Task<(bool Succeeded, WebSocketReceiveResult Result)> TryReceiveAsync(WebSocket source,
            WebSocket destination, byte[] buffer, CancellationToken cancellationToken)
        {
            try
            {
                var result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                return (true, result);
            }
            catch (OperationCanceledException)
            {
                await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, null, cancellationToken);
                return (false, default);
            }
            catch (WebSocketException e)
            {
                if (e.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely) throw;
                await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, null, cancellationToken);
                return (false, default);
            }
        }

        public static async Task Invoke(HttpContext httpContext)
        {
            var uri = httpContext.Items.DownstreamRequest().ToUri();
            await Proxy(httpContext, uri);
        }

        private static async Task Proxy(HttpContext context, string serverEndpoint)
        {
            Validate(context, serverEndpoint);

            var client = new ClientWebSocket();
            foreach (var protocol in context.WebSockets.WebSocketRequestedProtocols)
            {
                client.Options.AddSubProtocol(protocol);
            }

            SetHeaders(context, client);

            var destinationUri = new Uri(serverEndpoint);
            await client.ConnectAsync(destinationUri, context.RequestAborted);
            using var server = await context.WebSockets.AcceptWebSocketAsync(client.SubProtocol);
            await Task.WhenAll(PumpWebSocket(client, server, DefaultWebSocketBufferSize, context.RequestAborted),
                PumpWebSocket(server, client, DefaultWebSocketBufferSize, context.RequestAborted));
        }

        private static void SetHeaders(HttpContext context, ClientWebSocket client)
        {
            foreach (var headerEntry in context.Request.Headers)
            {
                if (NotForwardedWebSocketHeaders.Contains(headerEntry.Key, StringComparer.OrdinalIgnoreCase)) continue;
                TrySetHeader(client, headerEntry);
            }
        }

        private static void TrySetHeader(ClientWebSocket client, KeyValuePair<string, StringValues> headerEntry)
        {
            try
            {
                client.Options.SetRequestHeader(headerEntry.Key, headerEntry.Value);
            }
            catch (ArgumentException)
            {
                // Expected in .NET Framework for headers that are mistakenly considered restricted.
                // See: https://github.com/dotnet/corefx/issues/26627
                // .NET Core does not exhibit this issue, ironically due to a separate bug (https://github.com/dotnet/corefx/issues/18784)
            }
        }

        private static void Validate(HttpContext context, string serverEndpoint)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (serverEndpoint == null)
            {
                throw new ArgumentNullException(nameof(serverEndpoint));
            }

            if (!context.WebSockets.IsWebSocketRequest)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
