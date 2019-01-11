﻿using System.Reflection.Metadata.Ecma335;
using Ocelot.Requester;

namespace Ocelot.ManualTest
{
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.DependencyInjection;
    using Ocelot.Middleware;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Threading;

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
                        .AddJsonFile("ocelot.json", false, false)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices(s => {
                     s.AddAuthentication()
                        .AddJwtBearer("TestKey", x =>
                        {
                            x.Authority = "test";
                            x.Audience = "test";
                        });

                     s.AddSingleton<QosDelegatingHandlerDelegate>((x, t) => new FakeHandler());
                     s.AddOcelot()
                        .AddDelegatingHandler<FakeHandler>(true);
                        // .AddCacheManager(x =>
                        // {
                        //     x.WithDictionaryHandle();
                        // })
                        // .AddOpenTracing(option =>
                        // {
                        //     option.CollectorUrl = "http://localhost:9618";
                        //     option.Service = "Ocelot.ManualTest";
                        // })
                        // .AddAdministration("/administration", "secret");
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

    public class FakeHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Console.WriteLine(request.RequestUri);

            //do stuff and optionally call the base handler..
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
