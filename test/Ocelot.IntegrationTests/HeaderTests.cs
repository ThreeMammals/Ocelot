using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Net;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Ocelot.IntegrationTests
{
    public class HeaderTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private IWebHost _builder;
        private IWebHostBuilder _webHostBuilder;
        private readonly string _ocelotBaseUrl;
        private IWebHost _downstreamBuilder;
        private HttpResponseMessage _response;

        public HeaderTests()
        {
            _httpClient = new HttpClient();
            var port = PortFinder.GetRandomPort();
            _ocelotBaseUrl = $"http://localhost:{port}";
            _httpClient.BaseAddress = new Uri(_ocelotBaseUrl);
        }

        [Fact]
        public async Task Should_pass_remote_ip_address_if_as_x_forwarded_for_header()
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
                        UpstreamHeaderTransform = new Dictionary<string,string>
                        {
                            {"X-Forwarded-For", "{RemoteIpAddress}"},
                        },
                        HttpHandlerOptions = new FileHttpHandlerOptions
                        {
                            AllowAutoRedirect = false,
                        },
                    },
                },
            };

            GivenThereIsAServiceRunningOn($"http://localhost:{port}", 200, "X-Forwarded-For");
            GivenThereIsAConfiguration(configuration);
            GivenOcelotIsRunning();
            await WhenIGetUrlOnTheApiGateway("/");
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
            await ThenXForwardedForIsSet();
        }

        private void GivenThereIsAServiceRunningOn(string url, int statusCode, string headerKey)
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
                        if (context.Request.Headers.TryGetValue(headerKey, out var values))
                        {
                            var result = values.First();
                            context.Response.StatusCode = statusCode;
                            await context.Response.WriteAsync(result);
                        }
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
                .ConfigureServices(x => x.AddOcelot())
                .Configure(async app => await app.UseOcelot());

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

        private async Task WhenIGetUrlOnTheApiGateway(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            _response = await _httpClient.SendAsync(request);
        }

        private void ThenTheStatusCodeShouldBe(HttpStatusCode code)
        {
            _response.StatusCode.ShouldBe(code);
        }

        private async Task ThenXForwardedForIsSet()
        {
            var windowsOrMac = "::1";
            var linux = "127.0.0.1";
            var header = await _response.Content.ReadAsStringAsync();
            var passed = header == windowsOrMac || header == linux;
            passed.ShouldBeTrue();
        }

        public void Dispose()
        {
            _builder?.Dispose();
            _httpClient?.Dispose();
            _downstreamBuilder?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
