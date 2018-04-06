using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.WebSockets.Middleware
{
    public class WebSocketsProxyMiddleware : OcelotMiddleware
    {
        private OcelotRequestDelegate _next;

        public WebSocketsProxyMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory)
                :base(loggerFactory.CreateLogger<WebSocketsProxyMiddleware>())
        {
            _next = next;
        }

        public async Task Invoke(DownstreamContext context)
        {
            await Proxy(context.HttpContext, context.DownstreamRequest.ToUri());
        }

        private async Task Proxy(HttpContext context, string serverEndpoint)
        {
            var wsToUpstreamClient = await context.WebSockets.AcceptWebSocketAsync();

            var wsToDownstreamService = new ClientWebSocket();
            var uri = new Uri(serverEndpoint);
            await wsToDownstreamService.ConnectAsync(uri, CancellationToken.None);

            var receiveFromUpstreamSendToDownstream = Task.Run(async () =>
            {
                var buffer = new byte[1024 * 4];

                var receiveSegment = new ArraySegment<byte>(buffer);

                while (wsToUpstreamClient.State == WebSocketState.Open || wsToUpstreamClient.State == WebSocketState.CloseSent)
                {
                    var result = await wsToUpstreamClient.ReceiveAsync(receiveSegment, CancellationToken.None);

                    var sendSegment = new ArraySegment<byte>(buffer, 0, result.Count);

                    if(result.MessageType == WebSocketMessageType.Close)
                    {
                        await wsToUpstreamClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "",
                        CancellationToken.None);

                        await wsToDownstreamService.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "",
                        CancellationToken.None);
                            
                        break;
                    }

                    await wsToDownstreamService.SendAsync(sendSegment, result.MessageType, result.EndOfMessage,
                        CancellationToken.None);

                    if (wsToUpstreamClient.State != WebSocketState.Open)
                    {
                        await wsToDownstreamService.CloseAsync(WebSocketCloseStatus.Empty, "",
                            CancellationToken.None);
                        break;
                    }
                }
            });

            var receiveFromDownstreamAndSendToUpstream = Task.Run(async () =>
            {
                var buffer = new byte[1024 * 4];

                while (wsToDownstreamService.State == WebSocketState.Open || wsToDownstreamService.State == WebSocketState.CloseSent)
                {
                    if (wsToUpstreamClient.State != WebSocketState.Open)
                    {
                        break;
                    }
                    else
                    {
                        var receiveSegment = new ArraySegment<byte>(buffer);
                        var result = await wsToDownstreamService.ReceiveAsync(receiveSegment, CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }

                        var sendSegment = new ArraySegment<byte>(buffer, 0, result.Count);

                        //send to upstream client
                        await wsToUpstreamClient.SendAsync(sendSegment, result.MessageType, result.EndOfMessage,
                            CancellationToken.None);
                    }
                }
            });

            await Task.WhenAll(receiveFromDownstreamAndSendToUpstream, receiveFromUpstreamSendToDownstream);
        }
    }
}
