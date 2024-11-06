#if NET7_0_OR_GREATER
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Net;

namespace Ocelot.IntegrationTests
{
    public class RateLimitingTests
    {
        private const string _rateLimitPolicyName = "RateLimitPolicy";
        private const int _rateLimitLimit = 3;
        private const string _quotaExceededMessage = "woah!";
        private TestServer _testServer;
        private HttpClient _httpClient;

#if NET7_0_OR_GREATER
        [Fact]
        public async Task Should_RateLimit()
        {
            var port = PortFinder.GetRandomPort();
            
            var initialConfiguration = new FileConfiguration
            {
                GlobalConfiguration =
                    new FileGlobalConfiguration()
                    {
                        RateLimitOptions = new FileRateLimitOptions()
                        {
                            HttpStatusCode = 429, 
                            QuotaExceededMessage = _quotaExceededMessage,
                        },
                    },
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort> { new() { Host = "localhost", Port = port, } },
                        DownstreamScheme = "http",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/",
                        RateLimitOptions = new FileRateLimitRule()
                        {
                            EnableRateLimiting = true,
                            RateLimitMiddlewareType = RateLimitMiddlewareType.DotNet,
                            RateLimitPolicyName = _rateLimitPolicyName,
                        },
                    },
                },
            };
            await GivenThereIsAConfiguration(initialConfiguration);
            CreateOcelotServer();
            CreateDownstreamServer($"http://localhost:{port}");
            CreateHttpClient();

            var responses = await CallMultipleTimes(3);

            responses.ForEach(t =>
            {
                t.StatusCode.ShouldBe(HttpStatusCode.OK);
            });

            responses = await CallMultipleTimes(3);

            foreach (var t in responses)
            {
                t.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
                var body = await t.Content.ReadAsStringAsync();
                body.ShouldBe(_quotaExceededMessage);
            }
        }

        private void CreateOcelotServer()
        {
            var builder = new TestHostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                    config.AddJsonFile("ocelot.json", false, false);
                    config.AddEnvironmentVariables();
                })
                .Configure(app =>
                {
                    app.UseOcelot().Wait();
                })
                .ConfigureServices(services =>
                {
                    services.AddOcelot();
                    services.AddRateLimiter(op =>
                    {
                        op.AddFixedWindowLimiter(policyName: _rateLimitPolicyName, options =>
                        {
                            options.PermitLimit = _rateLimitLimit;
                            options.Window = TimeSpan.FromSeconds(12);
                            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                            options.QueueLimit = 0;
                        });
                    });
                })
                .UseUrls("http://localhost:5000");

            _testServer = new TestServer(builder);
        }

        private void CreateDownstreamServer(string url)
        {
            var builder = TestHostBuilder.Create()
                .UseUrls(url)
                .UseKestrel()
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync("result");
                    });
                })
                .Build();

            builder.Start();
        }

        private void CreateHttpClient()
        {
            _httpClient = _testServer.CreateClient();
        }

        private async Task<List<HttpResponseMessage>> CallMultipleTimes(int callCount)
        {
            var responses = new List<HttpResponseMessage>();
            for (var i = 0; i < callCount; i++)
            {
                var getResponse = await _httpClient.GetAsync("/");
                responses.Add(getResponse);
            }

            return responses;
        }

        private static async Task GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
        {
            var configurationPath = $"{Directory.GetCurrentDirectory()}/ocelot.json";

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            await File.WriteAllTextAsync(configurationPath, jsonConfiguration);

            _ = await File.ReadAllTextAsync(configurationPath);

            configurationPath = $"{AppContext.BaseDirectory}/ocelot.json";

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            await File.WriteAllTextAsync(configurationPath, jsonConfiguration);

            _ = await File.ReadAllTextAsync(configurationPath);
        }
#endif
    }
}
