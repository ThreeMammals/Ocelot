using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.ResponseCompression;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Collections.Generic;
using Xunit;
using TestStack.BDDfy;

namespace Ocelot.AcceptanceTests.ServerSentEvents;

public class ServerSentEventsTests : Steps
{
    private readonly List<string> _receivedEvents = new();
    private readonly Stopwatch _stopwatch = new();
    
    [Fact]
    [Trait("Feat", "941")]
    public void Should_proxy_server_sent_events_without_buffering()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/sse", "/sse");
        var configuration = GivenConfiguration(route);

        this.Given(x => GivenThereIsAnSseServiceRunningOn(port, "/sse"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithCompression())
            .When(x => WhenIConnectToTheApiGatewaySseEndpointSync("/sse"))
            .Then(x => ThenTheEventsAreReceivedInRealTime())
            .BDDfy();
    }



    private void GivenThereIsAnSseServiceRunningOn(int port, string basePath)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, async context =>
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Connection", "keep-alive");

            await context.Response.WriteAsync("data: event1\n\n");
            await context.Response.Body.FlushAsync();

            // Wait deliberately to ensure chunks are sent asynchronously
            await Task.Delay(1000);

            await context.Response.WriteAsync("data: event2\n\n");
            await context.Response.Body.FlushAsync();
        });
    }

    private void WhenIConnectToTheApiGatewaySseEndpointSync(string url)
    {
        WhenIConnectToTheApiGatewaySseEndpoint(url).GetAwaiter().GetResult();
    }

    private async Task WhenIConnectToTheApiGatewaySseEndpoint(string url)
    {
        _stopwatch.Start();

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        response = await ocelotClient.SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);

        System.IO.File.AppendAllText("test-debug.log", $"Response StatusCode: {response.StatusCode}\n");
        System.IO.File.AppendAllText("test-debug.log", $"Response Content-Type: {response.Content.Headers.ContentType}\n");

        if (response.IsSuccessStatusCode)
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);

            while (true)
            {
                var line = await reader.ReadLineAsync();
                System.IO.File.AppendAllText("test-debug.log", $"Read line: '{line}'\n");
                if (line == null) break;
                if (!string.IsNullOrEmpty(line))
                {
                    _receivedEvents.Add(line);
                    
                    if (_receivedEvents.Count == 1)
                    {
                        _stopwatch.Stop();
                    }
                }
            }
        }
    }

    private void ThenTheEventsAreReceivedInRealTime()
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        _receivedEvents.Count.ShouldBeGreaterThanOrEqualTo(2);
        _receivedEvents[0].ShouldBe("data: event1");
        _receivedEvents[1].ShouldBe("data: event2");

        // The first event should be received way before the 1000ms delay finishes.
        _stopwatch.ElapsedMilliseconds.ShouldBeLessThan(500); 
    }
    private void ThenResponseIsCompressed()
    {
        response.Content.Headers.ContentEncoding.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Feat", "941")]
    public void Should_proxy_signalr_sse_without_buffering()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/signalr", "/signalr");
        var configuration = GivenConfiguration(route);

        this.Given(x => GivenThereIsASignalRSseServiceRunningOn(port, "/signalr"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIConnectToTheApiGatewaySseEndpointSync("/signalr"))
            .Then(x => ThenTheSignalREventsAreReceivedInRealTime())
            .BDDfy();
    }

    private void GivenThereIsASignalRSseServiceRunningOn(int port, string basePath)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, async context =>
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Connection", "keep-alive");

            // SignalR handshake chunk
            await context.Response.WriteAsync("data: {\"type\":0}\u001e\n\n");
            await context.Response.Body.FlushAsync();

            await Task.Delay(1000);

            // SignalR message chunk
            await context.Response.WriteAsync("data: {\"type\":1,\"target\":\"ReceiveMessage\",\"arguments\":[\"Hello\"]}\u001e\n\n");
            await context.Response.Body.FlushAsync();
        });
    }

    private void ThenTheSignalREventsAreReceivedInRealTime()
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        _receivedEvents.Count.ShouldBeGreaterThanOrEqualTo(2);
        _receivedEvents[0].ShouldBe("data: {\"type\":0}\u001e");
        _receivedEvents[1].ShouldBe("data: {\"type\":1,\"target\":\"ReceiveMessage\",\"arguments\":[\"Hello\"]}\u001e");

        _stopwatch.ElapsedMilliseconds.ShouldBeLessThan(500); 
    }

    [Fact]
    [Trait("Feat", "941")]
    public async Task Should_proxy_true_signalr_communication_without_buffering()
    {
        var downstreamPort = PortFinder.GetRandomPort();
        var route = GivenRoute(downstreamPort, "/hub/{everything}", "/hub/{everything}");
        route.UpstreamHttpMethod.Add("POST"); // Necessary for /hub/negotiate endpoint
        route.UpstreamHttpMethod.Add("OPTIONS");
        
        var routeGet = GivenRoute(downstreamPort, "/hub", "/hub");
        routeGet.UpstreamHttpMethod.Add("POST"); 
        
        var configuration = GivenConfiguration(route, routeGet);

        int ocelotPort = 0;
        HubConnection connection = null;

        this.Given(x => GivenThereIsARealSignalRHubDownstream(downstreamPort))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAndSetPort(out ocelotPort))
            .When(x => WhenIConnectRealSignalRClientAndSetConnection(ocelotPort, "/hub", out connection))
            .Then(x => ThenTheRealSignalREventsAreReceivedInstantly())
            .BDDfy();

        if (connection != null)
        {
            await connection.StopAsync();
            await connection.DisposeAsync();
        }
    }

    private void GivenOcelotIsRunningAndSetPort(out int ocelotPort)
    {
        ocelotPort = GivenOcelotIsRunning();
    }

    private void WhenIConnectRealSignalRClientAndSetConnection(int ocelotPort, string path, out HubConnection connection)
    {
        connection = WhenIConnectRealSignalRClient(ocelotPort, path).GetAwaiter().GetResult();
    }

    private void GivenThereIsARealSignalRHubDownstream(int port)
    {
        handler.GivenThereIsAServiceRunningOn(port,
            configureDelegate: null,
            configureLogging: null,
            configureServices: services =>
            {
                services.AddSignalR();
            },
            configureApp: app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<AcceptanceChatHub>("/hub");
                });
            },
            configureWebHost: null);
    }

    private async Task<HubConnection> WhenIConnectRealSignalRClient(int ocelotPort, string path)
    {
        var hubUrl = $"http://localhost:{ocelotPort}{path}";
        
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;
                if (ocelotServer != null) 
                {
                    options.HttpMessageHandlerFactory = _ => ocelotServer.CreateHandler();
                }
            })
            .Build();

        connection.On<string>("ReceiveMessage", message =>
        {
            _receivedEvents.Add(message);
            if (_receivedEvents.Count == 1)
            {
                _stopwatch.Stop();
            }
        });

        _stopwatch.Start();
        await connection.StartAsync();

        // Let the stream run briefly to receive hub connection responses
        await Task.Delay(1500);

        return connection;
    }

    private void GivenOcelotIsRunningWithCompression()
    {
        GivenOcelotIsRunning(
            null,
            services => {
                services.AddOcelot();
                services.AddResponseCompression(options => {
                    options.EnableForHttps = true;
                    options.MimeTypes = new[] { "text/event-stream" };
                });
            },
            app => {
                app.UseResponseCompression();
                app.UseOcelot().Wait();
            });
    }

    private void ThenTheRealSignalREventsAreReceivedInstantly()
    {
        _receivedEvents.Count.ShouldBeGreaterThan(0);
        _receivedEvents[0].ShouldBe("Delay finished");

        // The hub waits 500ms before sending, so a buffered response would easily take >1sec including delays.
        // If it isn't buffered, we'll see it as soon as the hub sends it (around 500ms-600ms).
        _stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1500);
    }
}

public class AcceptanceChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        
        // Simulating some processing
        await Task.Delay(500); 
        
        // This is sent independently via the persistent SSE stream 
        await Clients.Caller.SendAsync("ReceiveMessage", "Delay finished");
    }
}
