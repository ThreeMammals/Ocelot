using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.Extensions.Logging;
using Ocelot.Tracing.OpenTracing;
using Jaeger;
using Microsoft.Extensions.DependencyInjection;
using OpenTracing;
using OpenTracing.Util;

namespace OcelotOpenTracing
{
    internal static class Program
    {
        private static void Main(string[] args)
        {

            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseKestrel()
                        .ConfigureAppConfiguration((hostingContext, config) =>
                        {
                            config
                                .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                                .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json",
                                    optional: true, reloadOnChange: false)
                                .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
                                .AddEnvironmentVariables();
                        })
                        .ConfigureServices((context, services) =>
                        {
                            services
                                .AddOcelot()
                                .AddOpenTracing();

                            services.AddSingleton<ITracer>(sp =>
                            {
                                var loggerFactory = sp.GetService<ILoggerFactory>();
                                Configuration config = new Configuration(context.HostingEnvironment.ApplicationName, loggerFactory);

                                var tracer = config.GetTracer();
                                GlobalTracer.Register(tracer);
                                return tracer;
                            });

                        })
                        .ConfigureLogging(logging =>
                        {
                            logging.AddConsole();
                        })
                        .Configure(app =>
                        {
                            app.UseOcelot().Wait();
                        });
                })
                .Build()
                .Run();
        }
    }
}
