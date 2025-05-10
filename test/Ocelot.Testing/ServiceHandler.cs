using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Ocelot.Testing;

public class ServiceHandler : IDisposable
{
    private readonly List<IWebHost> _hosts = new();

    public void Dispose()
    {
        _hosts.ForEach(h => h?.Dispose());
        GC.SuppressFinalize(this);
    }

    public void GivenThereIsAServiceRunningOn(string baseUrl, RequestDelegate handler)
    {
        var host = TestHostBuilder.Create()
            .UseUrls(baseUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .Configure(app => app.Run(handler))
            .Build();
        _hosts.Add(host);
        host.Start();
    }

    public void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, RequestDelegate handler)
    {
        var host = TestHostBuilder.Create()
            .UseUrls(baseUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .Configure(app => app.UsePathBase(basePath).Run(handler))
            .Build();
        _hosts.Add(host);
        host.Start();
    }

    public void GivenThereIsAServiceRunningOnWithKestrelOptions(string baseUrl, string basePath, Action<KestrelServerOptions> options, RequestDelegate handler)
    {
        var host = TestHostBuilder.Create()
            .UseUrls(baseUrl)
            .UseKestrel()
            .ConfigureKestrel(options ?? WithDefaultKestrelServerOptions) // !
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .Configure(app => app.UsePathBase(basePath).Run(handler))
            .Build();
        _hosts.Add(host);
        host.Start();
    }

    internal void WithDefaultKestrelServerOptions(KestrelServerOptions options)
    {
    }

    public void GivenThereIsAHttpsServiceRunningOn(string baseUrl, string basePath, string fileName, string password, int port, RequestDelegate handler)
    {
        void WithKestrelOptions(KestrelServerOptions options)
        {
            options.Listen(IPAddress.Loopback, port, o => o.UseHttps(fileName, password));
        }
        var host = TestHostBuilder.Create()
            .UseUrls(baseUrl)
            .UseKestrel(WithKestrelOptions)
            .UseContentRoot(Directory.GetCurrentDirectory())
            .Configure(app => app.UsePathBase(basePath).Run(handler))
            .Build();
        _hosts.Add(host);
        host.Start();
    }
}
