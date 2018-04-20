namespace Ocelot.ManualTest
{
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.DependencyInjection;
    using Ocelot.Middleware;

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
                .ConfigureServices(s => {
                     s.AddAuthentication()
                        .AddJwtBearer("TestKey", x =>
                        {
                            x.Authority = "test";
                            x.Audience = "test";
                        });

                    s.AddOcelot()
                        .AddCacheManager(x =>
                        {
                            x.WithDictionaryHandle();
                        })
                      /*.AddOpenTracing(option =>
                      {
                          option.CollectorUrl = "http://localhost:9618";
                          option.Service = "Ocelot.ManualTest";
                      })*/
                    .AddAdministration("/administration", "secret");
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
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
}
