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
        private ConcurrentBag<ThreadSafeHeadersTestResult> _results;

        public ThreadSafeHeadersTests()
        {
            _results = new ConcurrentBag<ThreadSafeHeadersTestResult>();
            _random = new Random();
            _httpClient = new HttpClient();
            _ocelotBaseUrl = "http://localhost:5000";
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
                            UpstreamHttpMethod = "Get",
                        }
                    }
            };

            this.Given(x => GivenThereIsAConfiguration(configuration))
               .And(x => GivenThereIsAServiceRunningOn("http://localhost:51879"))
                .And(x => GivenOcelotIsRunning())
                .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesWithDifferentHeaderValues("/", 100))
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
        private void WhenIPostOnTheApiGateway(string url, FileConfiguration updatedConfiguration)
        {
            var json = JsonConvert.SerializeObject(updatedConfiguration);
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            _response = _httpClient.PostAsync(url, content).Result;
        }

        private void ThenTheResponseShouldBe(FileConfiguration expected)
        {
            var response = JsonConvert.DeserializeObject<FileConfiguration>(_response.Content.ReadAsStringAsync().Result);

            response.GlobalConfiguration.AdministrationPath.ShouldBe(expected.GlobalConfiguration.AdministrationPath);
            response.GlobalConfiguration.RequestIdKey.ShouldBe(expected.GlobalConfiguration.RequestIdKey);
            response.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            response.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Port);
            response.GlobalConfiguration.ServiceDiscoveryProvider.Provider.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Provider);

            for (var i = 0; i < response.ReRoutes.Count; i++)
            {
                response.ReRoutes[i].DownstreamHost.ShouldBe(expected.ReRoutes[i].DownstreamHost);
                response.ReRoutes[i].DownstreamPathTemplate.ShouldBe(expected.ReRoutes[i].DownstreamPathTemplate);
                response.ReRoutes[i].DownstreamPort.ShouldBe(expected.ReRoutes[i].DownstreamPort);
                response.ReRoutes[i].DownstreamScheme.ShouldBe(expected.ReRoutes[i].DownstreamScheme);
                response.ReRoutes[i].UpstreamPathTemplate.ShouldBe(expected.ReRoutes[i].UpstreamPathTemplate);
                response.ReRoutes[i].UpstreamHttpMethod.ShouldBe(expected.ReRoutes[i].UpstreamHttpMethod);
            }
        }

        private void GivenIHaveAddedATokenToMyRequest()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
        }

        private void GivenIHaveAnOcelotToken(string adminPath)
        {
            var tokenUrl = $"{adminPath}/connect/token";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "admin"),
                new KeyValuePair<string, string>("client_secret", "secret"),
                new KeyValuePair<string, string>("scope", "admin"),
                new KeyValuePair<string, string>("username", "admin"),
                new KeyValuePair<string, string>("password", "secret"),
                new KeyValuePair<string, string>("grant_type", "password")
            };
            var content = new FormUrlEncodedContent(formData);

            var response = _httpClient.PostAsync(tokenUrl, content).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            response.EnsureSuccessStatusCode();
            _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
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
