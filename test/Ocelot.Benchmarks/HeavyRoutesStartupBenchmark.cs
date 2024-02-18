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

        public HeavyRoutesStartupBenchmark()
        {
            AddColumn(StatisticColumn.AllStatistics);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddValidator(BaselineValidator.FailOnError);
        }

        [GlobalSetup]
        public void SetUp()
        {
            //Setting up for more than 600 routes in different files
            CreateOcelotConfigFile(0, 100, "ocelot.json");
            CreateOcelotConfigFile(101, 500, "ocelot.second.json");
        }
        
        [Benchmark]
        public void StartOcelotWithLargeConfigurations()
        {
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
                    config
                        .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddJsonFile("ocelot.json", false, false)
                        .AddJsonFile("ocelot.second.json", false, false)
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
