using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Eureka;
using Ocelot.Provider.Polly;
using Ocelot.Samples.Web;

namespace Ocelot.Samples.Eureka.ApiGateway;

public class Program
{
    public static void Main(string[] args)
    {
        OcelotHostBuilder.Create(args)
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
                s.AddOcelot()
                    .AddEureka()
                    .AddPolly();
            })
            .Configure(a =>
            {
                a.UseOcelot().Wait();
            })
            .Build()
            .Run();
    }
}
