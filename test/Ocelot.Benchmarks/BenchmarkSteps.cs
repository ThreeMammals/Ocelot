using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.File;
using System.Net;
using System.Runtime.CompilerServices;

namespace Ocelot.Benchmarks;

internal class BenchmarkSteps : AcceptanceSteps
{
    public new FileHostAndPort Localhost(int port) => base.Localhost(port) as FileHostAndPort;
    public new FileConfiguration GivenConfiguration(params object[] routes) => base.GivenConfiguration(routes) as FileConfiguration;
    public new FileRoute GivenDefaultRoute(int port) => base.GivenDefaultRoute(port) as FileRoute;
    public new FileRoute GivenCatchAllRoute(int port) => base.GivenCatchAllRoute(port) as FileRoute;
    public new FileRoute GivenRoute(int port, string upstream = null, string downstream = null) => base.GivenRoute(port, upstream, downstream) as FileRoute;

    public void GivenThereIsAServiceRunningOn(int port, string basePath, int statusCode,
        [CallerMemberName] string responseBody = null)
        => handler.GivenThereIsAServiceRunningOn(port, basePath,
            context =>
            {
                context.Response.StatusCode = statusCode;
                return context.Response.WriteAsync(responseBody);
            });
    public void GivenThereIsAServiceRunningOnKestrel(int port, string basePath, int statusCode,
        Action<KestrelServerOptions> configureKestrel,
        [CallerMemberName] string responseBody = null)
        => handler.GivenThereIsAServiceRunningOnWithKestrelOptions(DownstreamUrl(port), basePath, configureKestrel,
            context =>
            {
                context.Response.StatusCode = statusCode;
                return context.Response.WriteAsync(responseBody);
            });
    public void GivenThereIsAServiceRunningOnKestrel(int port, string basePath, Action<KestrelServerOptions> configureKestrel, RequestDelegate @delegate)
        => handler.GivenThereIsAServiceRunningOnWithKestrelOptions(DownstreamUrl(port), basePath, configureKestrel, @delegate);

    public int GivenOcelotIsRunning(Action<IWebHostBuilder> postConfigureHost)
        => GivenOcelotIsRunning(null, null, null, null, postConfigureHost, null, null);
    public int GivenOcelotIsRunning(Action<IApplicationBuilder> configureApp, Action<IWebHostBuilder> postConfigureHost)
        => GivenOcelotIsRunning(null, null, configureApp, null, postConfigureHost, null, null);
}
