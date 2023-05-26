using ApiGateway.ServiceDiscovery;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.ServiceDiscovery;

using System;

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
                ServiceDiscoveryFinderDelegate serviceDiscoveryFinder = (serviceProvider, config, downstreamRoute) =>
                {
                    return new MyServiceDiscoveryProvider(serviceProvider, config, downstreamRoute);
                };

                s.AddSingleton(serviceDiscoveryFinder);
                s.AddOcelot();
            })
            .Configure(a =>
            {
                a.UseOcelot().Wait();
            })
            .Build();
}
