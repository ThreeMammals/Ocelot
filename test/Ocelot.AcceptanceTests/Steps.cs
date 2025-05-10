using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.AcceptanceTests.Properties;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace Ocelot.AcceptanceTests;

public class Steps : AcceptanceSteps
{
    private readonly List<IWebHost> _hosts;

    public Steps() : base()
    {
        _hosts = new();
        BddfyConfig.Configure();
    }

    public override void Dispose()
    {
        _hosts.ForEach(h => h?.Dispose());
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    public void GivenOcelotIsRunningWithDelegatingHandler<THandler>(bool global = false)
        where THandler : DelegatingHandler
    {
        GivenOcelotIsRunning(s => s.AddOcelot().AddDelegatingHandler<THandler>(global));
    }

    public void GivenOcelotIsRunning(OcelotPipelineConfiguration pipelineConfig)
    {
        var builder = TestHostBuilder.Create()
            .ConfigureAppConfiguration(WithBasicConfiguration)
            .ConfigureServices(WithAddOcelot)
            .Configure(async a => await a.UseOcelot(pipelineConfig));
        ocelotServer = new TestServer(builder);
        ocelotClient = ocelotServer.CreateClient();
    }

    public Task StartWebSocketsDownstreamServiceAsync(string url, Func<HttpContext, Func<Task>, Task> middleware)
    {
        var host = TestHostBuilder.Create()
            .ConfigureServices(s => { })
            .UseKestrel()
            .UseUrls(url)
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                var env = hostingContext.HostingEnvironment;
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
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
                app.Use(middleware);
            })
            .UseIISIntegration()
            .Build();
        _hosts.Add(host);
        return host.StartAsync();
    }
}
