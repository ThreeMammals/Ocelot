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
    private readonly List<(string Text, long ElapsedMs)> _receivedEvents = [];
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

            // Wait deliberately to ensure chunks are sent asynchronously
            await Task.Delay(1000);

            await context.Response.WriteAsync("data: event2\n\n");

            // Big timeout after second event to show client not affected by end of handler
            await Task.Delay(5000);
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
                    _receivedEvents.Add((line, _stopwatch.ElapsedMilliseconds));
                    if (_receivedEvents.Count >= 2)
                    {
                        break;
                    }
                }
            }
        }
    }

    private void ThenTheEventsAreReceivedInRealTime()
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _receivedEvents.Count.ShouldBe(2);
        _receivedEvents[0].Text.ShouldBe("data: event1");
        _receivedEvents[1].Text.ShouldBe("data: event2");

        // First event must arrive early without flush
        _receivedEvents[0].ElapsedMs.ShouldBeLessThan(400);

        // Second event must arrive right after 1000ms delay, way before 5000ms handler end delay
        _receivedEvents[1].ElapsedMs.ShouldBeGreaterThanOrEqualTo(950);
        _receivedEvents[1].ElapsedMs.ShouldBeLessThan(2000);

        // Delta strictly around 1000ms proves true async streaming, not buffered batch or end of handler
        var delta = _receivedEvents[1].ElapsedMs - _receivedEvents[0].ElapsedMs;
        delta.ShouldBeGreaterThanOrEqualTo(900);
        delta.ShouldBeLessThan(1600);
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
