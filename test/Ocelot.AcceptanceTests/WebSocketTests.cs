using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class WebSocketTests : IDisposable
    {
        private IWebHost _firstDownstreamHost;
        private IWebHost _secondDownstreamHost;
        private readonly List<string> _secondRecieved;
        private readonly List<string> _firstRecieved;
        private readonly List<ServiceEntry> _serviceEntries;
        private readonly Steps _steps;
        private IWebHost _fakeConsulBuilder;

        public WebSocketTests()
        {
            _steps = new Steps();
            _firstRecieved = new List<string>();
            _secondRecieved = new List<string>();
            _serviceEntries = new List<ServiceEntry>();
        }

        [Fact]
        public void should_proxy_websocket_input_to_downstream_service()
        {
            var downstreamPort = 5001;
            var downstreamHost = "localhost";

            var config = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/",
                        DownstreamPathTemplate = "/ws",
                        DownstreamScheme = "ws",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = downstreamHost,
                                Port = downstreamPort
                            }
                        }
                    }
                }
            };

            this.Given(_ => _steps.GivenThereIsAConfiguration(config))
                .And(_ => _steps.StartFakeOcelotWithWebSockets())
                .And(_ => StartFakeDownstreamService($"http://{downstreamHost}:{downstreamPort}", "/ws"))
                .When(_ => StartClient("ws://localhost:5000/"))
                .Then(_ => _firstRecieved.Count.ShouldBe(10))
                .BDDfy();
        }

        [Fact]
        public void should_proxy_websocket_input_to_downstream_service_and_use_load_balancer()
        {
            var downstreamPort = 5005;
            var downstreamHost = "localhost";
            var secondDownstreamPort = 5006;
            var secondDownstreamHost = "localhost";

            var config = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/",
                        DownstreamPathTemplate = "/ws",
                        DownstreamScheme = "ws",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = downstreamHost,
                                Port = downstreamPort
                            },
                            new FileHostAndPort
                            {
                                Host = secondDownstreamHost,
                                Port = secondDownstreamPort
                            }
                        },
                        LoadBalancerOptions = new FileLoadBalancerOptions { Type = "RoundRobin" }
                    }
                }
            };

            this.Given(_ => _steps.GivenThereIsAConfiguration(config))
                .And(_ => _steps.StartFakeOcelotWithWebSockets())
                .And(_ => StartFakeDownstreamService($"http://{downstreamHost}:{downstreamPort}", "/ws"))
                .And(_ => StartSecondFakeDownstreamService($"http://{secondDownstreamHost}:{secondDownstreamPort}","/ws"))
                .When(_ => WhenIStartTheClients())
                .Then(_ => ThenBothDownstreamServicesAreCalled())
                .BDDfy();
        }

        [Fact]
        public void should_proxy_websocket_input_to_downstream_service_and_use_service_discovery_and_load_balancer()
        {
            var downstreamPort = 5007;
            var downstreamHost = "localhost";

            var secondDownstreamPort = 5008;
            var secondDownstreamHost = "localhost";

            var serviceName = "websockets";
            var consulPort = 8509;
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = serviceName,
                    Address = downstreamHost,
                    Port = downstreamPort,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[0]
                },
            };
            var serviceEntryTwo = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = serviceName,
                    Address = secondDownstreamHost,
                    Port = secondDownstreamPort,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[0]
                },
            };

            var config = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/",
                        DownstreamPathTemplate = "/ws",
                        DownstreamScheme = "ws",
                        LoadBalancerOptions = new FileLoadBalancerOptions { Type = "RoundRobin" },
                        ServiceName = serviceName,
                        UseServiceDiscovery = true
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Host = "localhost",
                        Port = consulPort,
                        Type = "consul"
                    }
                }
            };
            
            this.Given(_ => _steps.GivenThereIsAConfiguration(config))
                .And(_ => _steps.StartFakeOcelotWithWebSockets())
                .And(_ => GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl, serviceName))
                .And(_ => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .And(_ => StartFakeDownstreamService($"http://{downstreamHost}:{downstreamPort}", "/ws"))
                .And(_ => StartSecondFakeDownstreamService($"http://{secondDownstreamHost}:{secondDownstreamPort}", "/ws"))
                .When(_ => WhenIStartTheClients())
                .Then(_ => ThenBothDownstreamServicesAreCalled())
                .BDDfy();
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

        private void GivenThereIsAFakeConsulServiceDiscoveryProvider(string url, string serviceName)
        {
            _fakeConsulBuilder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        if (context.Request.Path.Value == $"/v1/health/service/{serviceName}")
                        {
                            await context.Response.WriteJsonAsync(_serviceEntries);
                        }
                    });
                })
                .Build();

            _fakeConsulBuilder.Start();
        }

        private async Task WhenIStartTheClients()
        {
            var firstClient = StartClient("ws://localhost:5000/");

            var secondClient = StartSecondClient("ws://localhost:5000/");

            await Task.WhenAll(firstClient, secondClient);
        }

        private async Task StartClient(string url)
        {
            var client = new ClientWebSocket();
            
            await client.ConnectAsync(new Uri(url), CancellationToken.None);

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
                        _firstRecieved.Add(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        break;
                    }
                }
            });

            await Task.WhenAll(sending, receiving);
        }

        private async Task StartSecondClient(string url)
        {
            await Task.Delay(500);

            var client = new ClientWebSocket();

            await client.ConnectAsync(new Uri(url), CancellationToken.None);

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
                        _secondRecieved.Add(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        break;
                    }
                }
            });

            await Task.WhenAll(sending, receiving);
        }

        private async Task StartFakeDownstreamService(string url, string path)
        {
            _firstDownstreamHost = new WebHostBuilder()
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
                    });
                })
                .UseIISIntegration().Build();
            await _firstDownstreamHost.StartAsync();
        }

        private async Task StartSecondFakeDownstreamService(string url, string path)
        {
            _secondDownstreamHost = new WebHostBuilder()
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
                    });
                })
                .UseIISIntegration().Build();
            await _secondDownstreamHost.StartAsync();
        }

        private async Task Echo(WebSocket webSocket)
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

        private async Task Message(WebSocket webSocket)
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

        public void Dispose()
        {
            _steps.Dispose();
            _firstDownstreamHost?.Dispose();
            _secondDownstreamHost?.Dispose();
            _fakeConsulBuilder?.Dispose();
        }
    }
}
