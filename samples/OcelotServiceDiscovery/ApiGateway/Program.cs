using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.ServiceDiscovery;

namespace Ocelot.Samples.ServiceDiscovery.ApiGateway;

using ServiceDiscovery;

public class Program
{
    public static void Main(string[] args)
    {
        BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
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
                    s.RemoveAll<IServiceDiscoveryProviderFactory>();
                    s.AddSingleton<IServiceDiscoveryProviderFactory, MyServiceDiscoveryProviderFactory>();

                    // Will not be called, but it is required for internal validators, aka life hack
                    s.AddSingleton<ServiceDiscoveryFinderDelegate>((serviceProvider, config, downstreamRoute)
                        => null);
                }

                s.AddOcelot();
            })
            .Configure(a =>
            {
                a.UseOcelot().Wait();
            })
            .Build();
}
