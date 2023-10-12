// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Modified https://github.com/aspnet/Proxy websockets class to use in Ocelot.

using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using System.Net.WebSockets;

namespace Ocelot.WebSockets
{
    public class WebSocketsProxyMiddleware : OcelotMiddleware
    {
        private static readonly string[] NotForwardedWebSocketHeaders = new[]
        {
            "Connection", "Host", "Upgrade",
            "Sec-WebSocket-Accept", "Sec-WebSocket-Protocol", "Sec-WebSocket-Key", "Sec-WebSocket-Version", "Sec-WebSocket-Extensions",
        };

        private const int DefaultWebSocketBufferSize = 4096;
        private readonly RequestDelegate _next;
        private readonly IWebSocketsFactory _factory;

        public const string IgnoredSslWarningFormat = $"You have ignored all SSL warnings by using {nameof(DownstreamRoute.DangerousAcceptAnyServerCertificateValidator)} for this downstream route! {nameof(DownstreamRoute.UpstreamPathTemplate)}: '{{0}}', {nameof(DownstreamRoute.DownstreamPathTemplate)}: '{{1}}'.";
        public const string InvalidSchemeWarningFormat = "Invalid scheme has detected which will be replaced! Scheme '{0}' of the downstream '{1}'.";

        public WebSocketsProxyMiddleware(IOcelotLoggerFactory loggerFactory,
            RequestDelegate next,
            IWebSocketsFactory factory)
            : base(loggerFactory.CreateLogger<WebSocketsProxyMiddleware>())
        {
            _next = next;
            _factory = factory;
        }

        private static async Task PumpWebSocket(WebSocket source, WebSocket destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            var buffer = new byte[bufferSize];
            while (true)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, null, cancellationToken);
                    return;
                }
                catch (WebSocketException e)
                {
                    if (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                    {
                        await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, null, cancellationToken);
                        return;
                    }

                    throw;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await destination.CloseOutputAsync(source.CloseStatus.Value, source.CloseStatusDescription, cancellationToken);
                    return;
                }

                await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, cancellationToken);
            }
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var downstreamRequest = httpContext.Items.DownstreamRequest();
            var downstreamRoute = httpContext.Items.DownstreamRoute();
            await Proxy(httpContext, downstreamRequest, downstreamRoute);
        }

        private async Task Proxy(HttpContext context, DownstreamRequest request, DownstreamRoute route)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

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

            foreach (var headerEntry in context.Request.Headers)
            {
                if (!NotForwardedWebSocketHeaders.Contains(headerEntry.Key, StringComparer.OrdinalIgnoreCase))
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
            }

            // Only Uris starting with 'ws://' or 'wss://' are supported in System.Net.WebSockets.ClientWebSocket
            var scheme = request.Scheme;
            if (!scheme.StartsWith(Uri.UriSchemeWs))
            {
                Logger.LogWarning(() => string.Format(InvalidSchemeWarningFormat, scheme, request.ToUri()));
                request.Scheme = scheme == Uri.UriSchemeHttp ? Uri.UriSchemeWs
                    : scheme == Uri.UriSchemeHttps ? Uri.UriSchemeWss : scheme;
            }

            var destinationUri = new Uri(request.ToUri());
            await client.ConnectAsync(destinationUri, context.RequestAborted);

            using (var server = await context.WebSockets.AcceptWebSocketAsync(client.SubProtocol))
            {
                await Task.WhenAll(
                    PumpWebSocket(client.ToWebSocket(), server, DefaultWebSocketBufferSize, context.RequestAborted),
                    PumpWebSocket(server, client.ToWebSocket(), DefaultWebSocketBufferSize, context.RequestAborted));
            }
        }
    }
}
