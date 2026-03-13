using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Runtime.CompilerServices;

namespace Ocelot.Benchmarks;

internal class BenchmarkSteps : AcceptanceSteps
{
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
