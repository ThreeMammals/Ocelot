using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Testing;
using Shouldly;
using System.Diagnostics;
using System.Net;
using TestStack.BDDfy;

namespace Ocelot.Acceptance;

public class ServerSentEventsTests : Steps
{
    private readonly List<string> _receivedEvents = [];
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

        if (response.IsSuccessStatusCode)
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);

            while (true)
            {
                var line = await reader.ReadLineAsync();
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

    private void GivenOcelotIsRunningWithCompression()
    {
        GivenOcelotIsRunning(
            null,
            services =>
            {
                services.AddOcelot();
                services.AddResponseCompression(options =>
                {
                    options.EnableForHttps = true;
                    options.MimeTypes = ["text/event-stream"];
                });
            },
            app =>
            {
                app.UseResponseCompression();
                app.UseOcelot().Wait();
            });
    }
}
