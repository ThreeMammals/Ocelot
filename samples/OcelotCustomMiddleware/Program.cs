using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ocelot;
using Ocelot.Middleware;
using Ocelot.DependencyInjection;

namespace OcelotCustomMiddleware
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("ocelot.json", false, false);
                })
                .ConfigureServices(s => s.AddOcelot())
                .Configure(app =>
                {
                    app.UseOcelot(new OcelotPipelineConfiguration(), (builder, configuration) => builder.BuildeCustomPipeline(configuration)).Wait();
                    // app.UseOcelot(configuration=>{}, (builder, configuration) => builder.BuildeCustomPipeline(configuration)).Wait();
                });
    }
}
