using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Net.Sockets;
using System.Net;
using Iced.Intel;
using Microsoft.AspNetCore.Routing;
using System.Runtime.Intrinsics.X86;
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
            CreateOcelotConfigFile(0,100,"ocelot.json");
            CreateOcelotConfigFile(101, 500, "ocelot.second.json");
        }

/* * Summary *
        BenchmarkDotNet v0.13.11, Windows 11 (10.0.22621.2861/22H2/2022Update/SunValley2)
  Intel Core i7-8650U CPU 1.90GHz(Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
  .NET SDK 8.0.100
  [Host]     : .NET 6.0.25 (6.0.2523.51912), X64 RyuJIT AVX2[AttachedDebugger]
  DefaultJob : .NET 6.0.25 (6.0.2523.51912), X64 RyuJIT AVX2
| Method                             | Mean    | Error    | StdDev   | StdErr   | Min     | Q1      | Median  | Q3      | Max     | Op/s   | Gen0       | Gen1      | Allocated |
|----------------------------------- |--------:|---------:|---------:|---------:|--------:|--------:|--------:|--------:|--------:|-------:|-----------:|----------:|----------:|
| StartOcelotWithLargeConfigurations | 5.350 s | 0.0519 s | 0.0460 s | 0.0123 s | 5.214 s | 5.339 s | 5.351 s | 5.376 s | 5.408 s | 0.1869 | 42000.0000 | 3000.0000 | 185.05 MB |//        BenchmarkDotNet v0.13.11, Windows 11 (10.0.22621.2861/22H2/2022Update/SunValley2)
  Intel Core i7-8650U CPU 1.90GHz(Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
  .NET SDK 8.0.100
  [Host]     : .NET 6.0.25 (6.0.2523.51912), X64 RyuJIT AVX2[AttachedDebugger]
  DefaultJob : .NET 6.0.25 (6.0.2523.51912), X64 RyuJIT AVX2
| Method                             | Mean    | Error    | StdDev   | StdErr   | Min     | Q1      | Median  | Q3      | Max     | Op/s   | Gen0       | Gen1      | Allocated |
|----------------------------------- |--------:|---------:|---------:|---------:|--------:|--------:|--------:|--------:|--------:|-------:|-----------:|----------:|----------:|
| StartOcelotWithLargeConfigurations | 5.350 s | 0.0519 s | 0.0460 s | 0.0123 s | 5.214 s | 5.339 s | 5.351 s | 5.376 s | 5.408 s | 0.1869 | 42000.0000 | 3000.0000 | 185.05 MB |
        */

        [Benchmark]
        public void StartOcelotWithLargeConfigurations()
        {
            OcelotStartup($"http://localhost:{FindAvailablePort()}");
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            // Stop the web host after benchmarking
            _ocelot.StopAsync().GetAwaiter().GetResult();
        }

        private int FindAvailablePort()
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener?.Stop();
            }
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
