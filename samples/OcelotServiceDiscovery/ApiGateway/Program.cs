using ApiGateway.ServiceDiscovery;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;

namespace ApiGateway;

public class Program
{
    public static void Main(string[] args)
    {
        BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseUrls("http://localhost:5000")
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config
                    .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json", true, true)
                    .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                    .AddJsonFile("ocelot.json", false, false)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices(s =>
            {
                // Initialize from app configuration or hardcode/choose the best option.
                bool easyWay = true;

                if (easyWay)
                {
                    // Option #1. Define custom finder delegate to instantiate custom provider
                    // by default factory which is ServiceDiscoveryProviderFactory
                    s.AddSingleton<ServiceDiscoveryFinderDelegate>((serviceProvider, config, downstreamRoute)
                        => new MyServiceDiscoveryProvider(serviceProvider, config, downstreamRoute));
                }
                else
                {
                    // Option #2. Abstract from default factory (ServiceDiscoveryProviderFactory) and from FinderDelegate,
                    // and build custom factory by implementation of the IServiceDiscoveryProviderFactory interface.
                    s.AddScoped<IServiceDiscoveryProviderFactory, MyServiceDiscoveryProviderFactory>();
                    s.AddScoped<IServiceDiscoveryProvider, MyServiceDiscoveryProvider>();
                }

                s.AddOcelot();
            })
            .Configure(a =>
            {
                a.UseOcelot().Wait();
            })
            .Build();
}
