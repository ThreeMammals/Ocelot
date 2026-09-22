using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;

namespace Ocelot.Testing;

// TODO 1. Refactor in future to make this class a base class of acceptance steps
// TODO 2. Develop async versions for each sync method
public class ServiceHandler : IDisposable
{
#if NET10_0_OR_GREATER
    private readonly ConcurrentDictionary<string, IHost> _hosts = new();
#else
    private readonly ConcurrentDictionary<string, IWebHost> _hosts = new();
#endif

    public void Dispose()
    {
        foreach (var kv in _hosts)
        {
            kv.Value?.Dispose();
        }
        _hosts.Clear();
        GC.SuppressFinalize(this);
    }

    protected Task AddOrStopAsync(
        string key,
#if NET10_0_OR_GREATER
        IHost host)
#else
        IWebHost host)
#endif
    {
        if (_hosts.TryAdd(key, host))
            return Task.CompletedTask;

        var h = _hosts.GetOrAdd(key, host);
        _hosts[key] = host;

        // Shutdown old host
        return h.StopAsync().ContinueWith(t => h.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
    }

    public Task ReleasePortAsync(params int[] ports)
    {
        List<Task> tasks = new(ports.Length);
        foreach (int port in ports)
        {
            var kv = _hosts.SingleOrDefault(x => new Uri(x.Key).Port == port);
            if (kv.Key is null || kv.Value is null)
                continue;

            var host = kv.Value;
            var task = host.StopAsync()
                .ContinueWith(t =>
                {
                    host.Dispose();
                    _hosts.TryRemove(kv);
                });
            tasks.Add(task);
        }
        return Task.WhenAll(tasks);
    }

#if NET10_0_OR_GREATER
    private static IHost CreateHost(Action<IWebHostBuilder> configureWeb)
#else
    private static IWebHost CreateHost(Action<IWebHostBuilder> configureWeb)
#endif
    {
#if NET10_0_OR_GREATER
        var host = TestHostBuilder.CreateHost()
            .ConfigureWebHost(configureWeb)
            .Build();
#else
        var builder = TestHostBuilder.Create();
        configureWeb(builder);
        var host = builder.Build();
#endif
        return host;
    }

#if NET10_0_OR_GREATER
    public IHost
#else
    public IWebHost
#endif
        GivenThereIsAServiceRunningOn(string baseUrl, RequestDelegate handler)
    {
        void ConfigureWeb(IWebHostBuilder builder) => builder
            .UseUrls(baseUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .Configure(app => app.Run(handler));
        var host = CreateHost(ConfigureWeb);
        AddOrStopAsync(baseUrl, host).GetAwaiter().GetResult();
        host.Start();
        return host;
    }

    public void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, RequestDelegate handler)
    {
        void ConfigureWeb(IWebHostBuilder builder) => builder
            .UseUrls(baseUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .Configure(app => app.UsePathBase(basePath).Run(handler));
        var host = CreateHost(ConfigureWeb);
        AddOrStopAsync(baseUrl, host).GetAwaiter().GetResult();
        host.Start();
    }

    public void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, Action<IServiceCollection> configureServices, RequestDelegate handler)
    {
        void ConfigureWeb(IWebHostBuilder builder) => builder
            .UseUrls(baseUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureServices(configureServices)
            .Configure(app => app.UsePathBase(basePath).Run(handler));
        var host = CreateHost(ConfigureWeb);
        AddOrStopAsync(baseUrl, host).GetAwaiter().GetResult();
        host.Start();
    }

    public void GivenThereIsAServiceRunningOnWithKestrelOptions(string baseUrl, string basePath, Action<KestrelServerOptions> options, RequestDelegate handler)
    {
        void ConfigureWeb(IWebHostBuilder builder) => builder
            .UseUrls(baseUrl)
            .UseKestrel()
            .ConfigureKestrel(options ?? WithDefaultKestrelServerOptions) // !
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .Configure(app => app.UsePathBase(basePath).Run(handler));
        var host = CreateHost(ConfigureWeb);
        AddOrStopAsync(baseUrl, host).GetAwaiter().GetResult();
        host.Start();
    }

    internal void WithDefaultKestrelServerOptions(KestrelServerOptions options)
    { }

    public void GivenThereIsAHttpsServiceRunningOn(string baseUrl, string basePath, string fileName, string password, int port, RequestDelegate handler)
    {
        void WithKestrelOptions(KestrelServerOptions options)
            => options.Listen(IPAddress.Loopback, port, o => o.UseHttps(fileName, password));
        void ConfigureWeb(IWebHostBuilder builder) => builder
            .UseUrls(baseUrl)
            .UseKestrel(WithKestrelOptions)
            .UseContentRoot(Directory.GetCurrentDirectory())
            .Configure(app => app.UsePathBase(basePath).Run(handler));
        var host = CreateHost(ConfigureWeb);
        AddOrStopAsync(baseUrl, host).GetAwaiter().GetResult();
        host.Start();
    }

    #region Advanced helpers

    public static string Localhost(int port) => $"{Uri.UriSchemeHttp}://localhost:{port}";

    public void GivenThereIsAServiceRunningOn(int port, RequestDelegate handler)
        => GivenThereIsAServiceRunningOn(Localhost(port), handler);

    public void GivenThereIsAServiceRunningOn(int port, string path, RequestDelegate handler)
        => GivenThereIsAServiceRunningOn(Localhost(port), path, handler);

    #endregion

    /// <summary>
    /// Creates and runs a service to be hosted on a Kestrel web server by default, additionally binding a <c>localhost</c> URL to listen on the <paramref name="port"/>.
    /// </summary>
    /// <remarks>Use the <paramref name="configureWebHost"/> parameter to run non-Kestrel servers.</remarks>
    /// <param name="port">The port to run the service on.</param>
    /// <param name="configureDelegate">Custom configuration method.</param>
    /// <param name="configureLogging">Custom logging configuration method.</param>
    /// <param name="configureServices">Custom services configuration method.</param>
    /// <param name="configureApp">Custom application configuration method.</param>
    /// <param name="configureWebHost">Custom web host configuration method.</param>
    /// <returns>An <see cref="IHost"/> object as an instance of a web server.</returns>
#if NET10_0_OR_GREATER
    public IHost
#else
    public IWebHost
#endif
    GivenThereIsAServiceRunningOn(int port,
        Action<WebHostBuilderContext, IConfigurationBuilder>? configureDelegate,
        Action<WebHostBuilderContext, ILoggingBuilder>? configureLogging,
        Action<IServiceCollection>? configureServices,
        Action<IApplicationBuilder>? configureApp,
        Action<IWebHostBuilder>? configureWebHost)
    => GivenThereIsAServiceRunningOn(Localhost(port), configureDelegate, configureLogging, configureServices, configureApp, configureWebHost);

    /// <summary>
    /// Creates and runs a service to be hosted on a Kestrel web server by default, additionally binding a <paramref name="baseUrl"/> to listen on.
    /// </summary>
    /// <remarks>Use the <paramref name="configureWebHost"/> parameter to run non-Kestrel servers.</remarks>
    /// <param name="baseUrl">The URL to run the service on.</param>
    /// <param name="configureDelegate">Custom configuration method.</param>
    /// <param name="configureLogging">Custom logging configuration method.</param>
    /// <param name="configureServices">Custom services configuration method.</param>
    /// <param name="configureApp">Custom application configuration method.</param>
    /// <param name="configureWebHost">Custom web host configuration method.</param>
    /// <returns>An <see cref="IHost"/> object as an instance of a web server.</returns>
#if NET10_0_OR_GREATER
    public IHost
#else
    public IWebHost
#endif
    GivenThereIsAServiceRunningOn(string baseUrl,
        Action<WebHostBuilderContext, IConfigurationBuilder>? configureDelegate,
        Action<WebHostBuilderContext, ILoggingBuilder>? configureLogging,
        Action<IServiceCollection>? configureServices,
        Action<IApplicationBuilder>? configureApp,
        Action<IWebHostBuilder>? configureWebHost)
    {
        return GivenThereIsAServiceRunningOnAsync(baseUrl, configureDelegate, configureLogging, configureServices, configureApp, configureWebHost)
            .GetAwaiter().GetResult();
    }

#if NET10_0_OR_GREATER
    /// <summary>
    /// Creates and runs a service to be hosted on a Kestrel web server by default, additionally binding a <c>localhost</c> URL to listen on the <paramref name="port"/>.
    /// </summary>
    /// <remarks>Use the <paramref name="configureWebHost"/> parameter to run non-Kestrel servers.</remarks>
    /// <param name="port">The port to run the service on.</param>
    /// <param name="configureDelegate">Custom configuration method.</param>
    /// <param name="configureLogging">Custom logging configuration method.</param>
    /// <param name="configureServices">Custom services configuration method.</param>
    /// <param name="configureApp">Custom application configuration method.</param>
    /// <param name="configureWebHost">Custom web host configuration method.</param>
    /// <returns>An <see cref="IHost"/> object as an instance of a web server.</returns>
    public Task<IHost>
#else
    /// <summary>
    /// Creates and runs a service to be hosted on a Kestrel web server by default, additionally binding a <c>localhost</c> URL to listen on the <paramref name="port"/>.
    /// </summary>
    /// <remarks>Use the <paramref name="configureWebHost"/> parameter to run non-Kestrel servers.</remarks>
    /// <param name="port">The port to run the service on.</param>
    /// <param name="configureDelegate">Custom configuration method.</param>
    /// <param name="configureLogging">Custom logging configuration method.</param>
    /// <param name="configureServices">Custom services configuration method.</param>
    /// <param name="configureApp">Custom application configuration method.</param>
    /// <param name="configureWebHost">Custom web host configuration method.</param>
    /// <returns>An <see cref="IWebHost"/> object as an instance of a web server.</returns>
    public Task<IWebHost>
#endif
    GivenThereIsAServiceRunningOnAsync(int port,
        Action<WebHostBuilderContext, IConfigurationBuilder>? configureDelegate,
        Action<WebHostBuilderContext, ILoggingBuilder>? configureLogging,
        Action<IServiceCollection>? configureServices,
        Action<IApplicationBuilder>? configureApp,
        Action<IWebHostBuilder>? configureWebHost) // TODO Add CancellationToken argument
        => GivenThereIsAServiceRunningOnAsync(Localhost(port), configureDelegate, configureLogging, configureServices, configureApp, configureWebHost);

#if NET10_0_OR_GREATER
    /// <summary>
    /// Creates and runs a service to be hosted on a Kestrel web server by default, additionally binding a <paramref name="baseUrl"/> to listen on.
    /// </summary>
    /// <remarks>Use the <paramref name="configureWebHost"/> parameter to run non-Kestrel servers.</remarks>
    /// <param name="baseUrl">The URL to run the service on.</param>
    /// <param name="configureDelegate">Custom configuration method.</param>
    /// <param name="configureLogging">Custom logging configuration method.</param>
    /// <param name="configureServices">Custom services configuration method.</param>
    /// <param name="configureApp">Custom application configuration method.</param>
    /// <param name="configureWebHost">Custom web host configuration method.</param>
    /// <returns>An <see cref="IHost"/> object as an instance of a web server.</returns>
    public Task<IHost>
#else
    /// <summary>
    /// Creates and runs a service to be hosted on a Kestrel web server by default, additionally binding a <paramref name="baseUrl"/> to listen on.
    /// </summary>
    /// <remarks>Use the <paramref name="configureWebHost"/> parameter to run non-Kestrel servers.</remarks>
    /// <param name="baseUrl">The URL to run the service on.</param>
    /// <param name="configureDelegate">Custom configuration method.</param>
    /// <param name="configureLogging">Custom logging configuration method.</param>
    /// <param name="configureServices">Custom services configuration method.</param>
    /// <param name="configureApp">Custom application configuration method.</param>
    /// <param name="configureWebHost">Custom web host configuration method.</param>
    /// <returns>An <see cref="IWebHost"/> object as an instance of a web server.</returns>
    public Task<IWebHost>
#endif
    GivenThereIsAServiceRunningOnAsync(string baseUrl,
        Action<WebHostBuilderContext, IConfigurationBuilder>? configureDelegate,
        Action<WebHostBuilderContext, ILoggingBuilder>? configureLogging,
        Action<IServiceCollection>? configureServices,
        Action<IApplicationBuilder>? configureApp,
        Action<IWebHostBuilder>? configureWebHost) // TODO Add CancellationToken argument
    {
        void ConfigureWeb(IWebHostBuilder builder)
        {
            if (configureDelegate != null) builder.ConfigureAppConfiguration(configureDelegate);
            if (configureLogging != null) builder.ConfigureLogging(configureLogging);
            if (configureServices != null) builder.ConfigureServices(configureServices);
            if (configureApp != null) builder.Configure(configureApp);
            builder.UseUrls(baseUrl);
            if (configureWebHost is null) builder.UseKestrel();
            else configureWebHost.Invoke(builder);
        }
        var host = CreateBuilder(ConfigureWeb).Build();
        return AddOrStopAsync(baseUrl, host)
            .ContinueWith(t => host.StartAsync())
            .ContinueWith(t => host, TaskContinuationOptions.ExecuteSynchronously);
    }

#if NET10_0_OR_GREATER
    private static IHostBuilder
#else
    private static IWebHostBuilder
#endif
    CreateBuilder(Action<IWebHostBuilder> configureWeb)
    {
#if NET10_0_OR_GREATER
        return TestHostBuilder.CreateHost()
            .ConfigureWebHost(configureWeb);
#else
        var builder = TestHostBuilder.Create();
        configureWeb(builder);
        return builder;
#endif
    }
}
