using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
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
            var routes = new List<FileRoute>();

            const int MaximumNumberOfRoutes = 100;
            for (int i = 0; i < MaximumNumberOfRoutes; i++)
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
            GivenThereIsAConfiguration(configuration);
        }

        [Benchmark]
        public void StartOcelotWithLargeConfigurations()
        {
            OcelotStartup("http://localhost:5000");
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            // Stop the web host after benchmarking
            _ocelot.StopAsync().GetAwaiter().GetResult();
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

        public static void GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
        {
            var configurationPath = Path.Combine(AppContext.BaseDirectory, "ocelot.json");

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);
        }
    }
}
