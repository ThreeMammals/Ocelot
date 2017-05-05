using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.ManualTest;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace Ocelot.IntegrationTests
{
    public class ThreadSafeHeadersTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private HttpResponseMessage _response;
        private IWebHost _builder;
        private IWebHostBuilder _webHostBuilder;
        private readonly string _ocelotBaseUrl;
        private BearerToken _token;
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
        public void should_return_same_response_for_each_different_header_under_load_to_downsteam_service()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            DownstreamPort = 51879,
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    }
            };

            this.Given(x => GivenThereIsAConfiguration(configuration))
               .And(x => GivenThereIsAServiceRunningOn("http://localhost:51879"))
                .And(x => GivenOcelotIsRunning())
                .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesWithDifferentHeaderValues("/", 300))
                .Then(x => ThenTheSameHeaderValuesAreReturnedByTheDownstreamService())
                .BDDfy();
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
                .ConfigureServices(x =>
                {
                    x.AddSingleton(_webHostBuilder);
                })
                .UseStartup<Startup>();

            _builder = _webHostBuilder.Build();

            _builder.Start();
        }

        private void GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
        {
            var configurationPath = $"{Directory.GetCurrentDirectory()}/configuration.json";

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);

            var text = File.ReadAllText(configurationPath);

            configurationPath = $"{AppContext.BaseDirectory}/configuration.json";

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);

            text = File.ReadAllText(configurationPath);
        }

        public void WhenIGetUrlOnTheApiGatewayMultipleTimesWithDifferentHeaderValues(string url, int times)
        {
            var tasks = new Task[times];

            for (int i = 0; i < times; i++)
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
            int result = int.Parse(content);
            var tshtr = new ThreadSafeHeadersTestResult(result, random);
            _results.Add(tshtr);
        }

        private void ThenTheSameHeaderValuesAreReturnedByTheDownstreamService()
        {
            foreach(var result in _results)
            {
                result.Result.ShouldBe(result.Random);
            }
        }
        public void Dispose()
        {
            _builder?.Dispose();
            _httpClient?.Dispose();
            _downstreamBuilder?.Dispose();
        }

        class ThreadSafeHeadersTestResult
        {
            public ThreadSafeHeadersTestResult(int result, int random)
            {
                Result = result;
                Random = random;

            }

            public int Result { get; private set; }
            public int Random { get; private set; }
        }
    }
}
