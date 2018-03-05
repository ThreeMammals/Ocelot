using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class WebSocketTests : IDisposable
    {
        private WebHostBuilder _ocelotBuilder;
        private IWebHost _ocelotHost;
        private IWebHost _downstreamHost;

        [Fact]
        // [Fact(Skip = "Do not run will loop forever at the moment need to handle close signal on proxy")]
        public async Task should_proxy_websocket_input_to_downstream_service()
        {
            await StartFakeOcelot();

            await StartFakeDownstreamService();

            //create the websockets client
            var client = new ClientWebSocket();
            
            await client.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);

            Console.WriteLine("Connected!");
            //int count = 0;
            //function that sends from the client to the server
            var sending = Task.Run(async () =>
            {
                string line = "test";
                for (int i = 0; i < 10; i++)
                {
                    var bytes = Encoding.UTF8.GetBytes(line);

                    await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                        CancellationToken.None);
                    await Task.Delay(1000);
                }
                /*while (true)
                {
                   
                    //count++;
                }*/

                await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            });

            //function that receives on the client from the server
            var receiving = Task.Run(async () =>
            {
                var buffer = new byte[1024 * 4];

                while (true)
                {
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                        Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, result.Count));

                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        break;
                    }
                }
            });

            await Task.WhenAll(sending, receiving);
        }

        private async Task StartFakeOcelot()
        {
//start fake ocelot
            _ocelotBuilder = new WebHostBuilder();
            _ocelotBuilder.ConfigureServices(s =>
            {
                s.AddSingleton(_ocelotBuilder);
                //s.AddOcelot();
            });
            _ocelotBuilder.UseKestrel()
                .UseUrls("http://localhost:5000")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("configuration.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                })
                .Configure(app =>
                {
                    app.UseWebSockets();
                    app.Use(async (context, next) =>
                    {
                        if (context.Request.Path == "/ws")
                        {
                            if (context.WebSockets.IsWebSocketRequest)
                            {
                                //WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                                //context.WebSockets.AcceptWebSocketAsync(Proxy("http://localhost:5001/ws"));

                                await Proxy(context, "ws://localhost:5001/ws");
                                //await Proxy(context, webSocket);
                            }
                            else
                            {
                                context.Response.StatusCode = 400;
                            }
                        }
                        else
                        {
                            await next();
                        }
                    });
                    //app.UseOcelot().Wait();
                })
                .UseIISIntegration();
            _ocelotHost = _ocelotBuilder.Build();
            await _ocelotHost.StartAsync();
        }

        private async Task StartFakeDownstreamService()
        {
//start fake downstream service
            _downstreamHost = new WebHostBuilder()
                .ConfigureServices(s => { }).UseKestrel()
                .UseUrls("http://localhost:5001")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                })
                .Configure(app =>
                {
                    app.UseWebSockets();
                    app.Use(async (context, next) =>
                    {
                        if (context.Request.Path == "/ws")
                        {
                            if (context.WebSockets.IsWebSocketRequest)
                            {
                                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                                await Echo(context, webSocket);
                            }
                            else
                            {
                                context.Response.StatusCode = 400;
                            }
                        }
                        else
                        {
                            await next();
                        }
                    });
                })
                .UseIISIntegration().Build();
            await _downstreamHost.StartAsync();
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
                    // MUST read if we want the state to get updated...
                    try
                    {
                        //receive from upstream
                        var result = await wsToUpstreamClient.ReceiveAsync(receiveSegment, CancellationToken.None);

                        var sendSegment = new ArraySegment<byte>(buffer, 0, result.Count);

                        //kill the things
                        if(result.MessageType == WebSocketMessageType.Close)
                        {
                            Console.WriteLine("receiveFromUpstreamSendToDownstream received closed from upstream service, break");

                            await wsToUpstreamClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "",
                            CancellationToken.None);
                            
                            break;
                        }
                        //send to downstream
                        await wsToDownstreamService.SendAsync(sendSegment, result.MessageType, result.EndOfMessage,
                            CancellationToken.None);
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        return;
                    }

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
                try
                {
                    var buffer = new byte[1024 * 4];

                    //loop forever unless state to upstream client closes
                    while (wsToDownstreamService.State == WebSocketState.Open || wsToDownstreamService.State == WebSocketState.CloseSent)
                    {
                        if (wsToUpstreamClient.State != WebSocketState.Open)
                        {
                            break;
                        }
                        else
                        {
                            //receive from downstream service 
                            var receiveSegment = new ArraySegment<byte>(buffer);
                            var result = await wsToDownstreamService.ReceiveAsync(receiveSegment, CancellationToken.None);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                Console.WriteLine("receiveFromDownstreamAndSendToUpstream received close from downstream service, break");
                                break;
                            }

                            var sendSegment = new ArraySegment<byte>(buffer, 0, result.Count);

                            //send to upstream client
                            await wsToUpstreamClient.SendAsync(sendSegment, result.MessageType, result.EndOfMessage,
                                CancellationToken.None);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            });

            await Task.WhenAll(receiveFromDownstreamAndSendToUpstream, receiveFromUpstreamSendToDownstream);
            //wsToDownstreamService.Dispose();
        }


 /*       private async Task Proxy(HttpContext context, WebSocket wsToUpstreamClient)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await wsToUpstreamClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await wsToUpstreamClient.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await wsToUpstreamClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await wsToUpstreamClient.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }*/

        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            try
            {
                var buffer = new byte[1024 * 4];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue)
                {
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        public void Dispose()
        {
            _ocelotHost?.Dispose();
        }
    }
}