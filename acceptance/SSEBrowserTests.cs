using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Testing;
using Shouldly;

namespace Ocelot.Acceptance;

public class SSEBrowserTests : IAsyncLifetime
{
    private WebApplication _downstreamApp;
    private WebApplication _gatewayApp;
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IPage _page;

    private int DownstreamPort;
    private int OcelotPort;

    public async ValueTask InitializeAsync()
    {
        DownstreamPort = PortFinder.GetRandomPort();
        OcelotPort = PortFinder.GetRandomPort();

        // Start Downstream
        var downstreamBuilder = WebApplication.CreateBuilder();
        downstreamBuilder.Services.AddSignalR();
        _downstreamApp = downstreamBuilder.Build();
        _downstreamApp.Urls.Add($"http://localhost:{DownstreamPort}");
        _downstreamApp.MapHub<SseHub>("/testhub");
        _downstreamApp.MapGet("/sse-plain", async ctx =>
        {
            ctx.Response.ContentType = "text/event-stream";
            await ctx.Response.WriteAsync("data: event1\n\n");
            await ctx.Response.Body.FlushAsync();
            await Task.Delay(1000);
            await ctx.Response.WriteAsync("data: event2\n\n");
            await ctx.Response.Body.FlushAsync();
        });
        _downstreamApp.MapGet("/not-sse", async ctx =>
        {
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.WriteAsync("chunk1");
            await ctx.Response.Body.FlushAsync();
            await Task.Delay(2000);
            await ctx.Response.WriteAsync("chunk2");
            await ctx.Response.Body.FlushAsync();
        });
        await _downstreamApp.StartAsync();

        // Start Gateway
        var gatewayBuilder = WebApplication.CreateBuilder();
        var ocelotConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Routes:0:DownstreamPathTemplate", "/{everything}" },
                { "Routes:0:DownstreamScheme", "http" },
                { "Routes:0:DownstreamHostAndPorts:0:Host", "localhost" },
                { "Routes:0:DownstreamHostAndPorts:0:Port", DownstreamPort.ToString() },
                { "Routes:0:UpstreamPathTemplate", "/proxy/{everything}" },
                { "Routes:0:UpstreamHttpMethod:0", "Get" },
                { "Routes:0:UpstreamHttpMethod:1", "Post" },
                { "Routes:0:UpstreamHttpMethod:2", "Options" },
            })
            .Build();

        gatewayBuilder.Services.AddOcelot(ocelotConfig);
        gatewayBuilder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.MimeTypes = ["text/plain", "text/event-stream"];
        });
        gatewayBuilder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(p => p
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed(_ => true)
                .AllowCredentials());
        });

        _gatewayApp = gatewayBuilder.Build();
        _gatewayApp.Urls.Add($"http://localhost:{OcelotPort}");
        _gatewayApp.UseResponseCompression();
        _gatewayApp.UseCors();

        await _gatewayApp.UseOcelot();
        await _gatewayApp.StartAsync();

        // Start Playwright
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
        _page = await _browser.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        if (_gatewayApp != null) await _gatewayApp.DisposeAsync();
        if (_downstreamApp != null) await _downstreamApp.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    [Trait("Feat", "941")]
    public async Task Sse_Streaming_Through_Ocelot_ShouldWorkInBrowser()
    {
        var event1Received = new TaskCompletionSource<long>();
        var event2Received = new TaskCompletionSource<long>();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        _page.Console += (_, e) =>
        {
            if (e.Text == "Event: event1") event1Received.TrySetResult(sw.ElapsedMilliseconds);
            if (e.Text == "Event: event2") event2Received.TrySetResult(sw.ElapsedMilliseconds);
        };

        var html = $@"
            <html>
            <body>
                <script>
                    const es = new EventSource('http://localhost:{OcelotPort}/proxy/sse-plain');
                    es.onmessage = e => console.log('Event: ' + e.data);
                </script>
            </body>
            </html>";

        await _page.SetContentAsync(html);

        var t1 = await event1Received.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        t1.ShouldBeLessThan(600);

        var t2 = await event2Received.Task.WaitAsync(TimeSpan.FromSeconds(4), Xunit.TestContext.Current.CancellationToken);
        t2.ShouldBeGreaterThanOrEqualTo(900);

        var delta = t2 - t1;
        delta.ShouldBeGreaterThanOrEqualTo(800);
    }

    [Fact]
    [Trait("Feat", "941")]
    public async Task Should_Buffer_Plain_Text_Streaming_Response()
    {
        IResponse response = null;
        _page.Response += (_, r) => { if (r.Url.Contains("not-sse")) response = r; };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _page.GotoAsync($"http://localhost:{OcelotPort}/proxy/not-sse");
        sw.Stop();

        response.ShouldNotBeNull();
        response.Headers.ContainsKey("content-encoding").ShouldBeTrue();
        sw.ElapsedMilliseconds.ShouldBeGreaterThan(2000);
    }

    [Fact]
    [Trait("Feat", "941")]
    public async Task SignalR_SSE_Through_Ocelot_ShouldWorkInBrowser()
    {
        var connectionStartedTcs = new TaskCompletionSource<bool>();
        _page.Console += (_, e) =>
        {
            if (e.Text == "Connection started") connectionStartedTcs.TrySetResult(true);
        };

        var html = $@"
            <html>
            <head>
                <script src='https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js'></script>
            </head>
            <body>
                <div id='log'></div>
                <script>
                    const connection = new signalR.HubConnectionBuilder()
                        .withUrl('http://localhost:{OcelotPort}/proxy/testhub', {{ transport: signalR.HttpTransportType.ServerSentEvents }})
                        .build();
                    connection.on('ReceiveMessage', msg => {{
                        const d = document.createElement('div');
                        d.textContent = 'Received: ' + msg;
                        document.getElementById('log').appendChild(d);
                    }});
                    connection.start().then(() => console.log('Connection started'));
                </script>
            </body>
            </html>";

        await _page.SetContentAsync(html);
        await connectionStartedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);

        var hub = _downstreamApp.Services.GetRequiredService<IHubContext<SseHub>>();
        await hub.Clients.All.SendAsync("ReceiveMessage", "Hello from Ocelot!", cancellationToken: Xunit.TestContext.Current.CancellationToken);

        var logText = await _page.Locator("#log").InnerTextAsync();
        logText.ShouldContain("Received: Hello from Ocelot!");
    }

    private class SseHub : Hub { }
}
