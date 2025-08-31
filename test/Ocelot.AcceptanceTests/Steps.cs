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

    private HttpStatusCode pMapStatus_StatusCode = HttpStatusCode.OK;
    private Func<string> pMapStatus_ResponseBody;
    protected virtual Task MapStatus(HttpContext context)
    {
        context.Response.StatusCode = (int)pMapStatus_StatusCode;
        return context.Response.WriteAsync(pMapStatus_ResponseBody?.Invoke() ?? string.Empty);
    }
    protected virtual void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, [CallerMemberName] string responseBody = "")
    {
        pMapStatus_StatusCode = statusCode;
        pMapStatus_ResponseBody = () => responseBody;
        handler.GivenThereIsAServiceRunningOn(port, MapStatus);
    }

    protected Func<string> pMapOK_ResponseBody;
    protected virtual Task MapOK(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        return context.Response.WriteAsync(pMapOK_ResponseBody?.Invoke() ?? string.Empty);
    }
    public virtual void GivenThereIsAServiceRunningOnPath(int port, string basePath, [CallerMemberName] string responseBody = "")
    {
        pMapOK_ResponseBody = () => responseBody;
        handler.GivenThereIsAServiceRunningOn(port, basePath, MapOK);
    }
    public virtual void GivenThereIsAServiceRunningOn(int port, string basePath, RequestDelegate requestDelegate)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, requestDelegate);
    }

    protected override FileHostAndPort Localhost(int port) => base.Localhost(port) as FileHostAndPort;
    protected override FileConfiguration GivenConfiguration(params object[] routes) => base.GivenConfiguration(routes) as FileConfiguration;
    protected override FileRoute GivenDefaultRoute(int port) => base.GivenDefaultRoute(port) as FileRoute;
    protected override FileRoute GivenCatchAllRoute(int port) => base.GivenCatchAllRoute(port) as FileRoute;
    protected override FileRoute GivenRoute(int port, string upstream = null, string downstream = null) => base.GivenRoute(port, upstream, downstream) as FileRoute;

    protected static FileRouteBox<FileRoute> Box(FileRoute route) => new(route);
}
