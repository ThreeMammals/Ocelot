using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.WebSockets;
using System.Net.WebSockets;
using System.Text;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

public sealed class ConsulWebSocketTests : Steps
{
    private readonly List<string> _secondRecieved;
    private readonly List<string> _firstRecieved;
    private readonly List<ServiceEntry> _serviceEntries;

    public ConsulWebSocketTests()
    {
        _firstRecieved = new List<string>();
        _secondRecieved = new List<string>();
        _serviceEntries = new List<ServiceEntry>();
    }

    public override void Dispose()
    {
        ocelotHost?.Dispose();
        base.Dispose();
    }

    [Fact]
    public void ShouldProxyWebsocketInputToDownstreamServiceAndUseServiceDiscoveryAndLoadBalancer()
    {
        var downstreamPort = PortFinder.GetRandomPort();
        var downstreamHost = "localhost";

        var secondDownstreamPort = PortFinder.GetRandomPort();
        var secondDownstreamHost = "localhost";

        var serviceName = "websockets";
        var consulPort = PortFinder.GetRandomPort();
        var serviceEntryOne = new ServiceEntry
        {
            Service = new AgentService
            {
                Service = serviceName,
                Address = downstreamHost,
                Port = downstreamPort,
                ID = Guid.NewGuid().ToString(),
                Tags = Array.Empty<string>(),
            },
        };
        var serviceEntryTwo = new ServiceEntry
        {
            Service = new AgentService
            {
                Service = serviceName,
                Address = secondDownstreamHost,
                Port = secondDownstreamPort,
                ID = Guid.NewGuid().ToString(),
                Tags = Array.Empty<string>(),
            },
        };

        var config = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    UpstreamPathTemplate = "/",
                    DownstreamPathTemplate = "/ws",
                    DownstreamScheme = "ws",
                    LoadBalancerOptions = new FileLoadBalancerOptions { Type = "RoundRobin" },
                    ServiceName = serviceName,
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = consulPort,
                    Type = "consul",
                },
            },
        };

        this.Given(_ => GivenThereIsAConfiguration(config))
            .And(_ => StartFakeOcelotWithWebSocketsWithConsul())
            .And(_ => GivenThereIsAFakeConsulServiceDiscoveryProvider(consulPort, serviceName))
            .And(_ => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
            .And(_ => StartFakeDownstreamService($"http://{downstreamHost}:{downstreamPort}", "/ws"))
            .And(_ => StartSecondFakeDownstreamService($"http://{secondDownstreamHost}:{secondDownstreamPort}", "/ws"))
            .When(_ => WhenIStartTheClients())
            .Then(_ => ThenBothDownstreamServicesAreCalled())
            .BDDfy();
    }

    private async Task StartFakeOcelotWithWebSocketsWithConsul()
    {
        var builder = TestHostBuilder.Create();
        builder
            .ConfigureServices(s =>
            {
                s.AddSingleton(builder);
                s.AddOcelot().AddConsul();
            })
            .UseKestrel()
            .UseUrls(DownstreamUrl(5000)) // TODO not good to have constant hardcoded port
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                var env = hostingContext.HostingEnvironment;
                config.AddJsonFile("appsettings.json", true, false)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, false);
                config.AddJsonFile(ocelotConfigFileName, false, false);
                config.AddEnvironmentVariables();
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            })
            .Configure(async app =>
            {
                app.UseWebSockets();
                await app.UseOcelot();
            })
            .UseIISIntegration();
        ocelotHost = builder.Build(); // new TestServer(_webHostBuilder); ???
        await ocelotHost.StartAsync();
    }

    private void ThenBothDownstreamServicesAreCalled()
    {
        _firstRecieved.Count.ShouldBe(10);
        _firstRecieved.ForEach(x =>
        {
            x.ShouldBe("test");
        });

        _secondRecieved.Count.ShouldBe(10);
        _secondRecieved.ForEach(x =>
        {
            x.ShouldBe("chocolate");
        });
    }

    private void GivenTheServicesAreRegisteredWithConsul(params ServiceEntry[] serviceEntries)
    {
        foreach (var serviceEntry in serviceEntries)
        {
            _serviceEntries.Add(serviceEntry);
        }
    }

    private void GivenThereIsAFakeConsulServiceDiscoveryProvider(int port, string serviceName)
    {
        Task MapServicePath(HttpContext context)
        {
            if (context.Request.Path.Value == $"/v1/health/service/{serviceName}")
            {
                var json = JsonConvert.SerializeObject(_serviceEntries);
                context.Response.Headers.Append("Content-Type", "application/json");
                return context.Response.WriteAsync(json);
            }
            return Task.CompletedTask;
        }
        handler.GivenThereIsAServiceRunningOn(port, MapServicePath);
    }

    private async Task WhenIStartTheClients()
    {
        var firstClient = StartClient("ws://localhost:5000/");

        var secondClient = StartSecondClient("ws://localhost:5000/");

        await Task.WhenAll(firstClient, secondClient);
    }

    private async Task StartClient(string url)
    {
        IClientWebSocket client = new ClientWebSocketProxy();

        await client.ConnectAsync(new Uri(url), CancellationToken.None);

        var sending = Task.Run(async () =>
        {
            var line = "test";
            for (var i = 0; i < 10; i++)
            {
                var bytes = Encoding.UTF8.GetBytes(line);

                await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                    CancellationToken.None);
                await Task.Delay(10);
            }

            await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        });

        var receiving = Task.Run(async () =>
        {
            var buffer = new byte[1024 * 4];

            while (true)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    _firstRecieved.Add(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (client.State != WebSocketState.Closed)
                    {
                        // Last version, the client state is CloseReceived
                        // Valid states are: Open, CloseReceived, CloseSent
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }

                    break;
                }
            }
        });

        await Task.WhenAll(sending, receiving);
    }

    private async Task StartSecondClient(string url)
    {
        await Task.Delay(500);

        IClientWebSocket client = new ClientWebSocketProxy();

        await client.ConnectAsync(new Uri(url), CancellationToken.None);

        var sending = Task.Run(async () =>
        {
            var line = "test";
            for (var i = 0; i < 10; i++)
            {
                var bytes = Encoding.UTF8.GetBytes(line);

                await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                    CancellationToken.None);
                await Task.Delay(10);
            }

            await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        });

        var receiving = Task.Run(async () =>
        {
            var buffer = new byte[1024 * 4];

            while (true)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    _secondRecieved.Add(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (client.State != WebSocketState.Closed)
                    {
                        // Last version, the client state is CloseReceived
                        // Valid states are: Open, CloseReceived, CloseSent
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }

                    break;
                }
            }
        });

        await Task.WhenAll(sending, receiving);
    }

    private Task StartFakeDownstreamService(string url, string path)
    {
        async Task TheMiddleware(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path == path)
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await Echo(webSocket);
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
        }
        return GivenWebSocketServiceIsRunningOnAsync(url, TheMiddleware);
    }

    private Task StartSecondFakeDownstreamService(string url, string path)
    {
        async Task The2ndMiddleware(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path == path)
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await Message(webSocket);
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
        }
        return GivenWebSocketServiceIsRunningOnAsync(url, The2ndMiddleware);
    }

    private static async Task Echo(WebSocket webSocket)
    {
        try
        {
            var buffer = new byte[1024 * 4];

            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

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

    private static async Task Message(WebSocket webSocket)
    {
        try
        {
            var buffer = new byte[1024 * 4];

            var bytes = Encoding.UTF8.GetBytes("chocolate");

            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(bytes), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
