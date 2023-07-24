using Jaeger;
using Microsoft.AspNetCore.Hosting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Tracing.OpenTracing;
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
                            services.AddSingleton(sp =>
                            {
                                var loggerFactory = sp.GetService<ILoggerFactory>();
                                var config = new Configuration(context.HostingEnvironment.ApplicationName, loggerFactory);

                                var tracer = config.GetTracer();
                                GlobalTracer.Register(tracer);
                                return tracer;
                            });

                            services
                                .AddOcelot()
                                .AddOpenTracing();
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
