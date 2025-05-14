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
    public Steps() : base()
    {
        BddfyConfig.Configure();
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

    public Task GivenWebSocketServiceIsRunningOnAsync(string url, Func<HttpContext, Func<Task>, Task> middleware) =>
        handler.GivenThereIsAServiceRunningOnAsync(url,
            (context, config) => config
                .SetBasePath(context.HostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", true, false)
                .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, false)
                .AddEnvironmentVariables(),
            (context, logging) => logging
                .AddConfiguration(context.Configuration.GetSection("Logging"))
                .AddConsole(),
            null, // no services
            app => app.UseWebSockets().Use(middleware),
            web => web.UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
        );
}
