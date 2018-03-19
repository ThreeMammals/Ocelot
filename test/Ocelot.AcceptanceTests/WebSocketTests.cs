using System;
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
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Shouldly;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class WebSocketTests : IDisposable
    {
        private IWebHost _downstreamHost;
        private List<string> _recieved;
        private Steps _steps;

        public WebSocketTests()
        {
            _steps = new Steps();
            _recieved = new List<string>();
        }

        [Fact]
        public async Task should_proxy_websocket_input_to_downstream_service()
        {
            _steps.GivenThereIsAConfiguration(new FileConfiguration());

            await _steps.StartFakeOcelotWithWebSockets();

            await StartFakeDownstreamService("http://localhost:5001", "/ws");

            await StartClient();

            _recieved.Count.ShouldBe(10);
        }

        private async Task StartClient()
        {
            var client = new ClientWebSocket();
            
            await client.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);

            var sending = Task.Run(async () =>
            {
                string line = "test";
                for (int i = 0; i < 10; i++)
                {
                    var bytes = Encoding.UTF8.GetBytes(line);

                    await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                        CancellationToken.None);
                    await Task.Delay(10);
                }

                await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            });

            var receiving = Task.Run(async () =>
            {
                var buffer = new byte[1024 * 4];

                while (true)
                {
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        _recieved.Add(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }

                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        break;
                    }
                }
            });

            await Task.WhenAll(sending, receiving);
        }
        private async Task StartFakeDownstreamService(string url, string path)
        {
            _downstreamHost = new WebHostBuilder()
                .ConfigureServices(s => { }).UseKestrel()
                .UseUrls(url)
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
                        if (context.Request.Path == path)
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
            _steps.Dispose();
            _downstreamHost?.Dispose();
        }
    }
}