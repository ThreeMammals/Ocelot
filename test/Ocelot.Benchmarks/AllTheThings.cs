using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Middleware;
using Ocelot.DependencyInjection;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes.Jobs;

namespace Ocelot.Benchmarks
{
    [Config(typeof(AllTheThings))]
    public class AllTheThings : ManualConfig
    {
        private IWebHost _service;
        private IWebHost _ocelot;
        private HttpClient _httpClient;

        public AllTheThings()
        {
            Add(StatisticColumn.AllStatistics);
        }

        [GlobalSetup]
        public void SetUp()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51879,
                                }
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    }
            };

            GivenThereIsAServiceRunningOn("http://localhost:51879", "/", 201, string.Empty);
            GivenThereIsAConfiguration(configuration);
            GivenOcelotIsRunning("http://localhost:5000");

            _httpClient = new HttpClient();
        }

        [Benchmark]
        public async Task Benchmark1()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000/");
            var response = await _httpClient.SendAsync(request);
        }

        // * Summary *
        // BenchmarkDotNet=v0.10.13, OS=macOS 10.12.6 (16G1212) [Darwin 16.7.0]
        // Intel Core i5-4278U CPU 2.60GHz (Haswell), 1 CPU, 4 logical cores and 2 physical cores
        // .NET Core SDK=2.1.4
        //   [Host]     : .NET Core 2.0.6 (CoreCLR 4.6.0.0, CoreFX 4.6.26212.01), 64bit RyuJIT
        //   DefaultJob : .NET Core 2.0.6 (CoreCLR 4.6.0.0, CoreFX 4.6.26212.01), 64bit RyuJIT


        //      Method |     Mean |     Error |    StdDev |    StdErr |      Min |       Q1 |   Median |       Q3 |      Max |  Op/s |
        // ----------- |---------:|----------:|----------:|----------:|---------:|---------:|---------:|---------:|---------:|------:|
        //  Benchmark1 | 2.087 ms | 0.0310 ms | 0.0290 ms | 0.0075 ms | 2.049 ms | 2.062 ms | 2.086 ms | 2.105 ms | 2.157 ms | 479.2 |

        private void GivenOcelotIsRunning(string url)
        {
            _ocelot = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(url)
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
                    s.AddOcelot();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                })
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.UseOcelot().Wait();
                })
                .Build();

            _ocelot.Start();
        }

        public void GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
        {
            var configurationPath = Path.Combine(AppContext.BaseDirectory, "ocelot.json");;

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);
        }
        
        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody)
        {
            _service = new WebHostBuilder()
                .UseUrls(baseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.UsePathBase(basePath);
                    app.Run(async context =>
                    {   
                        context.Response.StatusCode = statusCode;
                        await context.Response.WriteAsync(responseBody);
                    });
                })
                .Build();

            _service.Start();
        }
    }
}
