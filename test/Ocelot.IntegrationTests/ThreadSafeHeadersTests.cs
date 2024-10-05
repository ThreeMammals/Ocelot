using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Collections.Concurrent;

namespace Ocelot.IntegrationTests
{
    public class ThreadSafeHeadersTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private IWebHost _builder;
        private IWebHostBuilder _webHostBuilder;
        private readonly string _ocelotBaseUrl;
        private IWebHost _downstreamBuilder;
        private readonly Random _random;
        private readonly ConcurrentBag<ThreadSafeHeadersTestResult> _results;

        public ThreadSafeHeadersTests()
        {
            _results = new ConcurrentBag<ThreadSafeHeadersTestResult>();
            _random = new Random();
            _httpClient = new HttpClient();
            _ocelotBaseUrl = "http://localhost:5001";
            _httpClient.BaseAddress = new Uri(_ocelotBaseUrl);
        }

        [Fact]
        public void Should_return_same_response_for_each_different_header_under_load_to_downsteam_service()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            GivenThereIsAConfiguration(configuration);
            GivenThereIsAServiceRunningOn($"http://localhost:{port}");
            GivenOcelotIsRunning();
            WhenIGetUrlOnTheApiGatewayMultipleTimesWithDifferentHeaderValues("/", 300);
            ThenTheSameHeaderValuesAreReturnedByTheDownstreamService();
        }

        private void GivenThereIsAServiceRunningOn(string url)
        {
            _downstreamBuilder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        var header = context.Request.Headers["ThreadSafeHeadersTest"];

                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync(header[0]);
                    });
                })
                .Build();

            _downstreamBuilder.Start();
        }

        private void GivenOcelotIsRunning()
        {
            _webHostBuilder = new WebHostBuilder()
                .UseUrls(_ocelotBaseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                    config.AddJsonFile("ocelot.json", false, false);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(x =>
                {
                    x.AddOcelot();
                })
                .Configure(async app =>
                {
                    await app.UseOcelot();
                });

            _builder = _webHostBuilder.Build();

            _builder.Start();
        }

        private static void GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
        {
            var configurationPath = $"{Directory.GetCurrentDirectory()}/ocelot.json";

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);

            _ = File.ReadAllText(configurationPath);

            configurationPath = $"{AppContext.BaseDirectory}/ocelot.json";

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);

            _ = File.ReadAllText(configurationPath);
        }

        private void WhenIGetUrlOnTheApiGatewayMultipleTimesWithDifferentHeaderValues(string url, int times)
        {
            var tasks = new Task[times];

            for (var i = 0; i < times; i++)
            {
                var urlCopy = url;
                var random = _random.Next(0, 50);
                tasks[i] = GetForThreadSafeHeadersTest(urlCopy, random);
            }

            Task.WaitAll(tasks);
        }

        private async Task GetForThreadSafeHeadersTest(string url, int random)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("ThreadSafeHeadersTest", new List<string> { random.ToString() });
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            var result = int.Parse(content);
            var tshtr = new ThreadSafeHeadersTestResult(result, random);
            _results.Add(tshtr);
        }

        private void ThenTheSameHeaderValuesAreReturnedByTheDownstreamService()
        {
            foreach (var result in _results)
            {
                result.Result.ShouldBe(result.Random);
            }
        }

        public void Dispose()
        {
            _builder?.Dispose();
            _httpClient?.Dispose();
            _downstreamBuilder?.Dispose();
            GC.SuppressFinalize(this);
        }

        private class ThreadSafeHeadersTestResult
        {
            public ThreadSafeHeadersTestResult(int result, int random)
            {
                Result = result;
                Random = random;
            }

            public int Result { get; }
            public int Random { get; }
        }
    }
}
