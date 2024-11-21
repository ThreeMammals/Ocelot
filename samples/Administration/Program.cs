using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ocelot.Administration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Samples.Web;

namespace Ocelot.Samples.AdministrationApi;

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
                   .AddJsonFile("ocelot.json")
                   .AddEnvironmentVariables();
           })
           .ConfigureServices(s =>
           {
               s.AddOcelot()
                   .AddAdministration("/administration", "secret");
           })
           .ConfigureLogging((hostingContext, logging) =>
           {
               logging.AddConsole();
           })
           .UseIISIntegration()
           .Configure(app =>
           {
               app.UseOcelot().Wait();
           })
           .Build()
           .Run();
    }
}
