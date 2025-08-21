using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.AcceptanceTests.Properties;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Runtime.CompilerServices;

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

    protected virtual void GivenThereIsAServiceRunningOn(int port, [CallerMemberName] string responseBody = "")
        => GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, responseBody);

    protected virtual void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, [CallerMemberName] string responseBody = "")
    {
        Task MapStatus(HttpContext context)
        {
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(responseBody);
        }
        handler.GivenThereIsAServiceRunningOn(port, MapStatus);
    }

    protected override FileHostAndPort Localhost(int port) => base.Localhost(port) as FileHostAndPort;
    protected override FileConfiguration GivenConfiguration(params object[] routes) => base.GivenConfiguration(routes) as FileConfiguration;
    protected override FileRoute GivenDefaultRoute(int port) => base.GivenDefaultRoute(port) as FileRoute;
    protected override FileRoute GivenCatchAllRoute(int port) => base.GivenCatchAllRoute(port) as FileRoute;
    protected override FileRoute GivenRoute(int port, string upstream = null, string downstream = null) => base.GivenRoute(port, upstream, downstream) as FileRoute;

    protected static FileRouteBox<FileRoute> Box(FileRoute route) => new(route);
}
