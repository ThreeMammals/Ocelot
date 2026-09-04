using Microsoft.AspNetCore.SignalR;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.ManualTest;
using System.Diagnostics;

namespace Ocelot.ManualTest.Actions;


/// <summary>
/// After installing https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/hosting-bundle?view=aspnetcore-10.0 ANCM, copy the schema file to the IIS Express installation directory:
/// copy "C:\Windows\System32\inetsrv\config\schema\aspnetcore_schema_v2.xml" "C:\Program Files\IIS Express\config\schema\"
/// </summary>
public static class Issue941
{
    private const int GatewayPort = 5000;
    private const int DownstreamPort = 5021;
    private const string IisExpressEnvVar = "OCELOT_ISSUE941_GATEWAY";

    public static async Task RunAsync()
    {
        await using var downstream = await StartDownstream();
        await using var gateway = await StartGateway();
        
        Console.WriteLine($"\nOcelot Gateway (Kestrel): http://localhost:{GatewayPort}");
        Console.WriteLine("Press ENTER to stop...");
        Console.ReadLine();
    }

    public static async Task RunWithIisExpressAsync()
    {
        IisExpressBootstrap.KillPort(GatewayPort);
        IisExpressBootstrap.KillPort(DownstreamPort);
        await Task.Delay(500);

        await using var downstream = await StartDownstream();

        var projectDir = Directory.GetCurrentDirectory();
        var publishDir = Path.Combine(projectDir, "bin", "iis-publish");

        using var iis = await IisExpressBootstrap.LaunchAsync(
            projectDir,
            publishDir,
            GatewayPort,
            IisExpressEnvVar,
            "1",
            "Ocelot.ManualTest.dll");

        Console.WriteLine("Press ENTER to stop IIS Express and Downstream...");
        Console.ReadLine();
        
        try { iis.Kill(); } catch { }
    }

    public static async Task RunAsGatewayAsync()
    {
        var builder = WebApplication.CreateBuilder();
        var ocelotConfig = BuildOcelotConfig();
        builder.Services.AddOcelot(ocelotConfig);
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        await using var app = builder.Build();
        app.UseCors();
        await app.UseOcelot();
        await app.RunAsync();
    }

    public static bool IsGatewayMode() =>
        Environment.GetEnvironmentVariable(IisExpressEnvVar) == "1";

    private static async Task<WebApplication> StartDownstream()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
        var app = builder.Build();
        app.Urls.Add($"http://localhost:{DownstreamPort}");
        app.MapHub<SseHub>("/testhub");

        app.MapGet("/sse-plain", async ctx =>
        {
            ctx.Response.ContentType = "text/event-stream";
            for (int i = 1; i <= 10; i++)
            {
                await ctx.Response.WriteAsync($"data: Event {i}\n\n");
                await Task.Delay(500);
            }
        });

        app.MapGet("/text-plain", async ctx =>
        {
            ctx.Response.ContentType = "text/plain";
            for (int i = 1; i <= 10; i++)
            {
                await ctx.Response.WriteAsync($"Chunk {i} ");
                await Task.Delay(500);
            }
        });

        app.UseCors();
        await app.StartAsync();
        Console.WriteLine($"Downstream: http://localhost:{DownstreamPort}");
        return app;
    }

    private static async Task<WebApplication> StartGateway()
    {
        var builder = WebApplication.CreateBuilder();
        var ocelotConfig = BuildOcelotConfig();
        builder.Services.AddOcelot(ocelotConfig);
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        var app = builder.Build();
        app.Urls.Add($"http://localhost:{GatewayPort}");
        app.UseCors();
        await app.UseOcelot();
        await app.StartAsync();
        return app;
    }

    private static IConfiguration BuildOcelotConfig() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Routes:0:DownstreamPathTemplate", "/{everything}" },
            { "Routes:0:DownstreamScheme", "http" },
            { "Routes:0:DownstreamHostAndPorts:0:Host", "localhost" },
            { "Routes:0:DownstreamHostAndPorts:0:Port", DownstreamPort.ToString() },
            { "Routes:0:UpstreamPathTemplate", "/proxy/{everything}" },
            { "Routes:0:UpstreamHttpMethod:0", "Get" },
            { "Routes:0:DownstreamHeaderTransform:Access-Control-Allow-Origin", "*" },
        })
        .Build();

    private class SseHub : Hub { }
}
