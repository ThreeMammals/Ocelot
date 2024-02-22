using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Testing;
namespace Ocelot.Benchmarks
{
    [Config(typeof(HeavyRoutesStartupBenchmark))]
    public class HeavyRoutesStartupBenchmark : ManualConfig
    {
        private IWebHost _ocelot;

        //7 files with 80+ routes
        int startFileCount = 0;

        //7 files with 80+ routes
        int endFileCount = 90;
        public HeavyRoutesStartupBenchmark()
        {
            AddColumn(StatisticColumn.AllStatistics);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddValidator(BaselineValidator.FailOnError);
        }

        [GlobalSetup]
        public void SetUp()
        {
            for (int i = 1; i <= 7; i++)
            {
                CreateOcelotConfigFile(startFileCount, endFileCount, $"ocelot{i}.json");
                startFileCount = endFileCount + 1;
                endFileCount += 90;
            }
        }

        [Benchmark]
        public void StartOcelotWithLargeConfigurations()
        {
            //Adding new file
            CreateOcelotConfigFile(startFileCount, endFileCount, $"ocelot{8}.json");
            OcelotStartup($"http://localhost:{TcpPortFinder.FindAvailablePort()}");
        }

        [Benchmark]
        public void StartOcelotWithOverrideFile()
        {   
            //Overriding with old file
            CreateOcelotConfigFile(0, 90, $"ocelot{8}.json");
            OcelotStartup($"http://localhost:{TcpPortFinder.FindAvailablePort()}");
        }

        [GlobalCleanup]
        public async void Cleanup()
        {
            // Stop the web host after benchmarking
            await _ocelot.StopAsync();
        }

        /// <summary>
        /// To replicate large number of routes.
        /// </summary>
        /// <param name="start">Starting digit to create downstream/upstream files.</param>
        /// <param name="end">Ending digit to create downstream/upstream files.</param>
        /// <param name="fileName">Ocelot files names.</param>
        private static void CreateOcelotConfigFile(int start, int end, string fileName)
        {
            var routes = new List<FileRoute>();

            for (int i = start; i < end; i++)
            {
                routes.Add(new()
                {
                    DownstreamPathTemplate = $"/downstream{i}",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = 51879,
                                },
                            },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = $"/upstream{i}",
                    UpstreamHttpMethod = new List<string> { "Get" },
                });
            }

            var configuration = new FileConfiguration { Routes = routes };
            GivenThereIsAConfiguration(configuration, fileName);
        }

        private void OcelotStartup(string url)
        {
            _ocelot = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(url)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    for (int i = 1; i <= 8; i++)
                    {
                        config.AddJsonFile($"ocelot{i}.json", false, false);
                    }

                    config
                        .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
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

        public static void GivenThereIsAConfiguration(FileConfiguration fileConfiguration, string fileName)
        {
            var configurationPath = Path.Combine(AppContext.BaseDirectory, fileName);

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);
        }
    }
}
