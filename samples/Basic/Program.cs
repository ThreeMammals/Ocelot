using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Ocelot.Samples.OcelotBasic.ApiGateway;

public class Program
{
    public static void Main(string[] args)
    {
        new WebHostBuilder()
           .UseKestrel()
           .UseContentRoot(Directory.GetCurrentDirectory())
           .ConfigureAppConfiguration((hostingContext, config) =>
           {
               config
                   .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                   .AddJsonFile("appsettings.json", true, true)
                   .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                   .AddJsonFile("ocelot.json")
                   .AddEnvironmentVariables();
           })
           .ConfigureLogging((hostingContext, logging) =>
           {
               if (hostingContext.HostingEnvironment.IsDevelopment())
               {
                   logging.ClearProviders();
                   logging.AddConsole();
               }
               //add your logging
           })
           .UseIISIntegration()
           .UseStartup<Startup>()
           .Build()
           .Run();
    }
}
