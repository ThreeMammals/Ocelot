using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public class SocketSend
    {
        public SocketSend(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage)
        {
            Buffer = buffer;
            this.MessageType = messageType;
            this.EndOfMessage = endOfMessage;
        }

        public ArraySegment<byte> Buffer { get; private set; }
        public WebSocketMessageType MessageType { get; private set; }
        public bool EndOfMessage { get; private set; }
    }

    public class SocketResult
    {
        public SocketResult(byte[] result, WebSocketMessageType messageType, bool endOfMessage, int count)
        {
            Count = count;
            this.Result = result;
            this.MessageType = messageType;
            this.EndOfMessage = endOfMessage;
        }

        public byte[] Result { get; private set; }
        public WebSocketMessageType MessageType { get; private set; }
        public bool EndOfMessage { get; private set; }
        public int Count {get;private set;}
    }

    public class SocketQueue
    {
        private ConcurrentQueue<SocketSend> _queue;
        private Thread _processing;

        public string Name { get; }

        private CancellationToken _cancellationToken;
        private WebSocket _socket;

        public SocketQueue(WebSocket socket, CancellationToken cancellationToken, string name)
        {
            Name = name;
            _cancellationToken = cancellationToken;
            _socket = socket;
            _queue = new ConcurrentQueue<SocketSend>();
            Responses = new ConcurrentQueue<SocketResult>();
            _processing = new Thread(async () => await Process());
            _processing.Start();
        }

        public bool Processing { get; private set; }

        public ConcurrentQueue<SocketResult> Responses { get; private set; }

        public void Send(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage)
        {
            Console.WriteLine($"{Name} Enqueued message");
            _queue.Enqueue(new SocketSend(buffer, messageType, endOfMessage));
        }

        private async Task Process()
        {
            try
            {
                Processing = true;

                while (Processing)
                {
                    if (_queue.TryPeek(out var next))
                    {
                        Console.WriteLine($"{Name} Sending message...");
                        await _socket.SendAsync(next.Buffer, next.MessageType, next.EndOfMessage, _cancellationToken);
                    }

                    var buffer = new byte[1024 * 4];
                    var response = await _socket.ReceiveAsync(buffer, _cancellationToken);

                    if (response.MessageType == WebSocketMessageType.Text)
                    {
                        var data = Encoding.UTF8.GetString(buffer, 0, response.Count);
                        Console.WriteLine($"{Name} Received message...{data}");
                        byte[] dest = new byte[buffer.Length]; 
                        Array.Copy(buffer, dest, buffer.Length);
                        Responses.Enqueue(new SocketResult(dest, response.MessageType, response.EndOfMessage, response.Count));
                        Console.WriteLine($"{Name} Enqueued message...{data}");
                    }
                    else if (response.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"{Name} Closing socket...");
                        Processing = false;
                        await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }

                    Thread.Sleep(50);
                    Console.WriteLine($"{Name} Finished loop...");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public class WebSocketTests : IDisposable
    {
        private WebHostBuilder _ocelotBuilder;
        private IWebHost _ocelotHost;
        private IWebHost _downstreamHost;
        private SocketQueue _clientQueue;
        private SocketQueue _upstreamQueue;
        private SocketQueue _downstreamQueue;
        private Thread _receiveFromUpstreamSendToDownstream;
        private Thread _receiveFromDownstreamAndSendToUpstream;

        public WebSocketTests()
        {
        }

        [Fact]
        public async Task should_proxy_websocket_input_to_downstream_service()
        {
            await StartFakeOcelot();

            await StartFakeDownstreamService();

            var client = new ClientWebSocket();

            await client.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);

            _clientQueue = new SocketQueue(client, CancellationToken.None, "upstreamclient");

            var sending = Task.Run(async () =>
            {
                string line = "test";
                for (int i = 0; i < 1; i++)
                {
                    var bytes = Encoding.UTF8.GetBytes(line);

                    _clientQueue.Send(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true);

                    await Task.Delay(50);
                }
            });

            var receiving = Task.Run(async () =>
            {
                var buffer = new byte[1024 * 4];
                int count = 0;
                while (count < 1)
                {
                    if (_clientQueue.Responses.TryDequeue(out var message))
                    {
                        Console.WriteLine(message.Result);
                        count++;
                    }

                    await Task.Delay(50);
                }
            });

            await Task.WhenAll(sending, receiving);

            Console.WriteLine("Fin!");
        }

        private async Task StartFakeOcelot()
        {
            _ocelotBuilder = new WebHostBuilder();
            _ocelotBuilder.ConfigureServices(s =>
            {
                s.AddSingleton(_ocelotBuilder);
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
                                await Proxy(context, "ws://localhost:5001/ws");
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
            //upstream
            var wsToUpstreamClient = await context.WebSockets.AcceptWebSocketAsync();
            _upstreamQueue = new SocketQueue(wsToUpstreamClient, CancellationToken.None, "upstreamproxy");

            //downstream
            var wsToDownstreamService = new ClientWebSocket();
            var uri = new Uri(serverEndpoint);
            await wsToDownstreamService.ConnectAsync(uri, CancellationToken.None);
            _downstreamQueue = new SocketQueue(wsToDownstreamService, CancellationToken.None, "downstreamproxy");

            _receiveFromUpstreamSendToDownstream = new Thread(async () =>
            {
                while (_downstreamQueue.Processing && _upstreamQueue.Processing)
                {
                    //Console.WriteLine($"receiveFromUpstreamSendToDownstream proccesing");

                    if (_upstreamQueue.Responses.TryDequeue(out var result))
                    {
                        Console.WriteLine($"receiveFromUpstreamSendToDownstream dequeued");
                        var data = Encoding.UTF8.GetString(result.Result, 0, result.Count);
                        Console.WriteLine($"receiveFromUpstreamSendToDownstream {data}");
                        var sendSegment = new ArraySegment<byte>(result.Result, 0, result.Count);
                        _downstreamQueue.Send(sendSegment, result.MessageType, result.EndOfMessage);
                    }

                    await Task.Delay(50);
                }
            });
            _receiveFromUpstreamSendToDownstream.Start();

            _receiveFromDownstreamAndSendToUpstream = new Thread(async () =>
            {
                while (_downstreamQueue.Processing && _upstreamQueue.Processing)
                {
                    //Console.WriteLine($"receiveFromDownstreamAndSendToUpstream proccesing");

                    if (_downstreamQueue.Responses.TryDequeue(out var result))
                    {
                        Console.WriteLine($"receiveFromDownstreamAndSendToUpstream dequeued");
                        var sendSegment = new ArraySegment<byte>(result.Result, 0, result.Count);
                        _upstreamQueue.Send(sendSegment, result.MessageType, result.EndOfMessage);
                    }

                    await Task.Delay(50);
                }
            });
        }

        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            try
            {
                var buffer = new byte[1024 * 4];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue && result.MessageType != WebSocketMessageType.Close)
                {
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                while(webSocket.State != WebSocketState.Closed)
                {
                    Console.WriteLine("Waiting for socket to close");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Dispose()
        {
            _ocelotHost?.Dispose();
            _downstreamHost?.Dispose();
        }
    }
}