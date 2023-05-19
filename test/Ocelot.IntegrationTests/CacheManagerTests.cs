namespace Ocelot.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    using Administration;

    using CacheManager.Core;

    using Configuration.File;

    using DependencyInjection;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    using Middleware;

    using Newtonsoft.Json;

    using Ocelot.Cache.CacheManager;

    using Shouldly;

    using TestStack.BDDfy;

    using Xunit;

    public class CacheManagerTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClient _httpClientTwo;
        private HttpResponseMessage _response;
        private IHost _builder;
        private IHostBuilder _webHostBuilder;
        private readonly string _ocelotBaseUrl;
        private BearerToken _token;

        public CacheManagerTests()
        {
            _httpClient = new HttpClient();
            _httpClientTwo = new HttpClient();
            _ocelotBaseUrl = "http://localhost:5000";
            _httpClient.BaseAddress = new Uri(_ocelotBaseUrl);
        }

        [Fact]
        public void should_clear_region()
        {
            var initialConfiguration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration(),
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = 80,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/",
                        FileCacheOptions = new FileCacheOptions
                        {
                            TtlSeconds = 10,
                        },
                    },
                    new()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = 80,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/test",
                        FileCacheOptions = new FileCacheOptions
                        {
                            TtlSeconds = 10,
                        },
                    },
                },
            };

            var regionToClear = "gettest";

            this.Given(x => GivenThereIsAConfiguration(initialConfiguration))
                .And(x => GivenOcelotIsRunning())
                .And(x => GivenIHaveAnOcelotToken("/administration"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .When(x => WhenIDeleteOnTheApiGateway($"/administration/outputcache/{regionToClear}"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NoContent))
                .BDDfy();
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
                new("client_id", "admin"),
                new("client_secret", "secret"),
                new("scope", "admin"),
                new("grant_type", "client_credentials"),
            };
            var content = new FormUrlEncodedContent(formData);

            var response = _httpClient.PostAsync(tokenUrl, content).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            response.EnsureSuccessStatusCode();
            _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
            var configPath = $"{adminPath}/.well-known/openid-configuration";
            response = _httpClient.GetAsync(configPath).Result;
            response.EnsureSuccessStatusCode();
        }

        private void GivenOcelotIsRunning()
        {
            _webHostBuilder = Host.CreateDefaultBuilder()
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
                    Action<ConfigurationBuilderCachePart> settings = (s) =>
                    {
                        s.WithMicrosoftLogging(log =>
                        {
                            //log.AddConsole(LogLevel.Debug);
                        })
                        .WithDictionaryHandle();
                    };
                    x.AddMvc(option => option.EnableEndpointRouting = false);
                    x.AddOcelot()
                    .AddCacheManager(settings)
                    .AddAdministration("/administration", "secret");
                })
                .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseUrls(_ocelotBaseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Configure(app =>
                {
                    app.UseOcelot().Wait();
                });
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

            var text = File.ReadAllText(configurationPath);

            configurationPath = $"{AppContext.BaseDirectory}/ocelot.json";

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);

            text = File.ReadAllText(configurationPath);
        }

        private void WhenIDeleteOnTheApiGateway(string url)
        {
            _response = _httpClient.DeleteAsync(url).Result;
        }

        private void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("OCELOT_CERTIFICATE", string.Empty);
            Environment.SetEnvironmentVariable("OCELOT_CERTIFICATE_PASSWORD", string.Empty);
            _builder?.Dispose();
            _httpClient?.Dispose();
            //_identityServerBuilder?.Dispose();
        }
    }
}
